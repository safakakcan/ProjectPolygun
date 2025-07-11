using System;
using System.Collections.Generic;
using Mono.CecilX;
using Mono.CecilX.Cil;
using Mono.CecilX.Rocks;
using UnityEngine;
using Object = UnityEngine.Object;

// to use Mono.CecilX.Rocks here, we need to 'override references' in the
// Unity.Mirror.CodeGen assembly definition file in the Editor, and add CecilX.Rocks.
// otherwise we get an unknown import exception.

namespace Mirror.Weaver
{
    // not static, because ILPostProcessor is multithreaded
    public class Writers
    {
        // Writers are only for this assembly.
        // can't be used from another assembly, otherwise we will get:
        // "System.ArgumentException: Member ... is declared in another module and needs to be imported"
        private readonly AssemblyDefinition assembly;
        private readonly TypeDefinition GeneratedCodeClass;
        private readonly Logger Log;
        private readonly WeaverTypes weaverTypes;

        private readonly Dictionary<TypeReference, MethodReference> writeFuncs = new(new TypeReferenceComparer());

        public Writers(AssemblyDefinition assembly, WeaverTypes weaverTypes, TypeDefinition GeneratedCodeClass, Logger Log)
        {
            this.assembly = assembly;
            this.weaverTypes = weaverTypes;
            this.GeneratedCodeClass = GeneratedCodeClass;
            this.Log = Log;
        }

        public void Register(TypeReference dataType, MethodReference methodReference)
        {
            // sometimes we define multiple write methods for the same type.
            // for example:
            //   WriteInt()     // alwasy writes 4 bytes: should be available to the user for binary protocols etc.
            //   WriteVarInt()  // varint compression: we may want Weaver to always use this for minimal bandwidth
            // give the user a way to define the weaver prefered one if two exists:
            //   "[WeaverPriority]" attribute is automatically detected and prefered.
            var methodDefinition = methodReference.Resolve();
            var priority = methodDefinition.HasCustomAttribute<WeaverPriorityAttribute>();
            // if (priority) Log.Warning($"Weaver: Registering priority Write<{dataType.FullName}> with {methodReference.FullName}.", methodReference);

            // Weaver sometimes calls Register for <T> multiple times because we resolve assemblies multiple times.
            // if the function name is the same: always use the latest one.
            // if the function name differes: use the priority one.
            if (writeFuncs.TryGetValue(dataType, out var existingMethod) && // if it was already defined
                existingMethod.FullName != methodReference.FullName && // and this one is a different name
                !priority) // and it's not the priority one
                return; // then skip

            // we need to import type when we Initialize Writers so import here in case it is used anywhere else
            var imported = assembly.MainModule.ImportReference(dataType);
            writeFuncs[imported] = methodReference;
        }

        private void RegisterWriteFunc(TypeReference typeReference, MethodDefinition newWriterFunc)
        {
            Register(typeReference, newWriterFunc);
            GeneratedCodeClass.Methods.Add(newWriterFunc);
        }

        // Finds existing writer for type, if non exists trys to create one
        public MethodReference GetWriteFunc(TypeReference variable, ref bool WeavingFailed)
        {
            if (writeFuncs.TryGetValue(variable, out var foundFunc))
                return foundFunc;

            // this try/catch will be removed in future PR and make `GetWriteFunc` throw instead
            try
            {
                var importedVariable = assembly.MainModule.ImportReference(variable);
                return GenerateWriter(importedVariable, ref WeavingFailed);
            }
            catch (GenerateWriterException e)
            {
                Log.Error(e.Message, e.MemberReference);
                WeavingFailed = true;
                return null;
            }
        }

        //Throws GenerateWriterException when writer could not be generated for type
        private MethodReference GenerateWriter(TypeReference variableReference, ref bool WeavingFailed)
        {
            if (variableReference.IsByReference) throw new GenerateWriterException($"Cannot pass {variableReference.Name} by reference", variableReference);

            // Arrays are special, if we resolve them, we get the element type,
            // e.g. int[] resolves to int
            // therefore process this before checks below
            if (variableReference.IsArray)
            {
                if (variableReference.IsMultidimensionalArray()) throw new GenerateWriterException($"{variableReference.Name} is an unsupported type. Multidimensional arrays are not supported", variableReference);
                var elementType = variableReference.GetElementType();
                return GenerateCollectionWriter(variableReference, elementType, nameof(NetworkWriterExtensions.WriteArray), ref WeavingFailed);
            }

            if (variableReference.Resolve()?.IsEnum ?? false)
                // serialize enum as their base type
                return GenerateEnumWriteFunc(variableReference, ref WeavingFailed);

            // check for collections
            if (variableReference.Is(typeof(ArraySegment<>)))
            {
                var genericInstance = (GenericInstanceType)variableReference;
                var elementType = genericInstance.GenericArguments[0];

                return GenerateCollectionWriter(variableReference, elementType, nameof(NetworkWriterExtensions.WriteArraySegment), ref WeavingFailed);
            }

            if (variableReference.Is(typeof(List<>)))
            {
                var genericInstance = (GenericInstanceType)variableReference;
                var elementType = genericInstance.GenericArguments[0];

                return GenerateCollectionWriter(variableReference, elementType, nameof(NetworkWriterExtensions.WriteList), ref WeavingFailed);
            }

            if (variableReference.Is(typeof(HashSet<>)))
            {
                var genericInstance = (GenericInstanceType)variableReference;
                var elementType = genericInstance.GenericArguments[0];

                return GenerateCollectionWriter(variableReference, elementType, nameof(NetworkWriterExtensions.WriteHashSet), ref WeavingFailed);
            }

            // handle both NetworkBehaviour and inheritors.
            // fixes: https://github.com/MirrorNetworking/Mirror/issues/2939
            if (variableReference.IsDerivedFrom<NetworkBehaviour>() || variableReference.Is<NetworkBehaviour>()) return GetNetworkBehaviourWriter(variableReference);

            // check for invalid types
            var variableDefinition = variableReference.Resolve();
            if (variableDefinition == null) throw new GenerateWriterException($"{variableReference.Name} is not a supported type. Use a supported type or provide a custom writer", variableReference);
            if (variableDefinition.IsDerivedFrom<Component>()) throw new GenerateWriterException($"Cannot generate writer for component type {variableReference.Name}. Use a supported type or provide a custom writer", variableReference);
            if (variableReference.Is<Object>()) throw new GenerateWriterException($"Cannot generate writer for {variableReference.Name}. Use a supported type or provide a custom writer", variableReference);
            if (variableReference.Is<ScriptableObject>()) throw new GenerateWriterException($"Cannot generate writer for {variableReference.Name}. Use a supported type or provide a custom writer", variableReference);
            if (variableDefinition.HasGenericParameters) throw new GenerateWriterException($"Cannot generate writer for generic type {variableReference.Name}. Use a supported type or provide a custom writer", variableReference);
            if (variableDefinition.IsInterface) throw new GenerateWriterException($"Cannot generate writer for interface {variableReference.Name}. Use a supported type or provide a custom writer", variableReference);
            if (variableDefinition.IsAbstract) throw new GenerateWriterException($"Cannot generate writer for abstract class {variableReference.Name}. Use a supported type or provide a custom writer", variableReference);

            // generate writer for class/struct
            return GenerateClassOrStructWriterFunction(variableReference, ref WeavingFailed);
        }

        private MethodReference GetNetworkBehaviourWriter(TypeReference variableReference)
        {
            // all NetworkBehaviours can use the same write function
            if (writeFuncs.TryGetValue(weaverTypes.Import<NetworkBehaviour>(), out var func))
            {
                // register function so it is added to writer<T>
                // use Register instead of RegisterWriteFunc because this is not a generated function
                Register(variableReference, func);

                return func;
            }

            // this exception only happens if mirror is missing the WriteNetworkBehaviour method
            throw new MissingMethodException("Could not find writer for NetworkBehaviour");
        }

        private MethodDefinition GenerateEnumWriteFunc(TypeReference variable, ref bool WeavingFailed)
        {
            var writerFunc = GenerateWriterFunc(variable);

            var worker = writerFunc.Body.GetILProcessor();

            var underlyingWriter = GetWriteFunc(variable.Resolve().GetEnumUnderlyingType(), ref WeavingFailed);

            worker.Emit(OpCodes.Ldarg_0);
            worker.Emit(OpCodes.Ldarg_1);
            worker.Emit(OpCodes.Call, underlyingWriter);

            worker.Emit(OpCodes.Ret);
            return writerFunc;
        }

        private MethodDefinition GenerateWriterFunc(TypeReference variable)
        {
            var functionName = $"_Write_{variable.FullName}";
            // create new writer for this type
            var writerFunc = new MethodDefinition(functionName,
                MethodAttributes.Public |
                MethodAttributes.Static |
                MethodAttributes.HideBySig,
                weaverTypes.Import(typeof(void)));

            writerFunc.Parameters.Add(new ParameterDefinition("writer", ParameterAttributes.None, weaverTypes.Import<NetworkWriter>()));
            writerFunc.Parameters.Add(new ParameterDefinition("value", ParameterAttributes.None, variable));
            writerFunc.Body.InitLocals = true;

            RegisterWriteFunc(variable, writerFunc);
            return writerFunc;
        }

        private MethodDefinition GenerateClassOrStructWriterFunction(TypeReference variable, ref bool WeavingFailed)
        {
            var writerFunc = GenerateWriterFunc(variable);

            var worker = writerFunc.Body.GetILProcessor();

            if (!variable.Resolve().IsValueType)
                WriteNullCheck(worker, ref WeavingFailed);

            if (!WriteAllFields(variable, worker, ref WeavingFailed))
                return null;

            worker.Emit(OpCodes.Ret);
            return writerFunc;
        }

        private void WriteNullCheck(ILProcessor worker, ref bool WeavingFailed)
        {
            // if (value == null)
            // {
            //     writer.WriteBoolean(false);
            //     return;
            // }
            //

            var labelNotNull = worker.Create(OpCodes.Nop);
            worker.Emit(OpCodes.Ldarg_1);
            worker.Emit(OpCodes.Brtrue, labelNotNull);
            worker.Emit(OpCodes.Ldarg_0);
            worker.Emit(OpCodes.Ldc_I4_0);
            worker.Emit(OpCodes.Call, GetWriteFunc(weaverTypes.Import<bool>(), ref WeavingFailed));
            worker.Emit(OpCodes.Ret);
            worker.Append(labelNotNull);

            // write.WriteBoolean(true);
            worker.Emit(OpCodes.Ldarg_0);
            worker.Emit(OpCodes.Ldc_I4_1);
            worker.Emit(OpCodes.Call, GetWriteFunc(weaverTypes.Import<bool>(), ref WeavingFailed));
        }

        // Find all fields in type and write them
        private bool WriteAllFields(TypeReference variable, ILProcessor worker, ref bool WeavingFailed)
        {
            foreach (var field in variable.FindAllPublicFields())
            {
                var writeFunc = GetWriteFunc(field.FieldType, ref WeavingFailed);
                // need this null check till later PR when GetWriteFunc throws exception instead
                if (writeFunc == null) return false;

                var fieldRef = assembly.MainModule.ImportReference(field);

                worker.Emit(OpCodes.Ldarg_0);
                worker.Emit(OpCodes.Ldarg_1);
                worker.Emit(OpCodes.Ldfld, fieldRef);
                worker.Emit(OpCodes.Call, writeFunc);
            }

            return true;
        }

        private MethodDefinition GenerateCollectionWriter(TypeReference variable, TypeReference elementType, string writerFunction, ref bool WeavingFailed)
        {
            var writerFunc = GenerateWriterFunc(variable);

            var elementWriteFunc = GetWriteFunc(elementType, ref WeavingFailed);
            var intWriterFunc = GetWriteFunc(weaverTypes.Import<int>(), ref WeavingFailed);

            // need this null check till later PR when GetWriteFunc throws exception instead
            if (elementWriteFunc == null)
            {
                Log.Error($"Cannot generate writer for {variable}. Use a supported type or provide a custom writer", variable);
                WeavingFailed = true;
                return writerFunc;
            }

            var module = assembly.MainModule;
            var readerExtensions = module.ImportReference(typeof(NetworkWriterExtensions));
            var collectionWriter = Resolvers.ResolveMethod(readerExtensions, assembly, Log, writerFunction, ref WeavingFailed);

            var methodRef = new GenericInstanceMethod(collectionWriter);
            methodRef.GenericArguments.Add(elementType);

            // generates
            // reader.WriteArray<T>(array);

            var worker = writerFunc.Body.GetILProcessor();
            worker.Emit(OpCodes.Ldarg_0); // writer
            worker.Emit(OpCodes.Ldarg_1); // collection

            worker.Emit(OpCodes.Call, methodRef); // WriteArray

            worker.Emit(OpCodes.Ret);

            return writerFunc;
        }

        // Save a delegate for each one of the writers into Writer{T}.write
        internal void InitializeWriters(ILProcessor worker)
        {
            var module = assembly.MainModule;

            var genericWriterClassRef = module.ImportReference(typeof(Writer<>));

            var fieldInfo = typeof(Writer<>).GetField(nameof(Writer<object>.write));
            var fieldRef = module.ImportReference(fieldInfo);
            var networkWriterRef = module.ImportReference(typeof(NetworkWriter));
            var actionRef = module.ImportReference(typeof(Action<,>));
            var actionConstructorRef = module.ImportReference(typeof(Action<,>).GetConstructors()[0]);

            foreach (var kvp in writeFuncs)
            {
                var targetType = kvp.Key;
                var writeFunc = kvp.Value;

                // create a Action<NetworkWriter, T> delegate
                worker.Emit(OpCodes.Ldnull);
                worker.Emit(OpCodes.Ldftn, writeFunc);
                var actionGenericInstance = actionRef.MakeGenericInstanceType(networkWriterRef, targetType);
                var actionRefInstance = actionConstructorRef.MakeHostInstanceGeneric(assembly.MainModule, actionGenericInstance);
                worker.Emit(OpCodes.Newobj, actionRefInstance);

                // save it in Writer<T>.write
                var genericInstance = genericWriterClassRef.MakeGenericInstanceType(targetType);
                var specializedField = fieldRef.SpecializeField(assembly.MainModule, genericInstance);
                worker.Emit(OpCodes.Stsfld, specializedField);
            }
        }
    }
}