using System;
using System.IO;
using System.Runtime.CompilerServices;

namespace Mirror.SimpleWeb
{
    public static class MessageProcessor
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static byte FirstLengthByte(byte[] buffer)
        {
            return (byte)(buffer[1] & 0b0111_1111);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool NeedToReadShortLength(byte[] buffer)
        {
            var lenByte = FirstLengthByte(buffer);
            return lenByte == Constants.UshortPayloadLength;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool NeedToReadLongLength(byte[] buffer)
        {
            var lenByte = FirstLengthByte(buffer);
            return lenByte == Constants.UlongPayloadLength;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int GetOpcode(byte[] buffer)
        {
            return buffer[0] & 0b0000_1111;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int GetPayloadLength(byte[] buffer)
        {
            return GetMessageLength(buffer, 0, FirstLengthByte(buffer));
        }

        /// <summary>
        ///     Has full message been sent
        /// </summary>
        /// <param name="buffer"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Finished(byte[] buffer)
        {
            return (buffer[0] & 0b1000_0000) != 0;
        }

        public static void ValidateHeader(byte[] buffer, int maxLength, bool expectMask, bool opCodeContinuation = false)
        {
            var finished = Finished(buffer);
            var hasMask = (buffer[1] & 0b1000_0000) != 0; // true from clients, false from server, "All messages from the client to the server have this bit set"

            var opcode = buffer[0] & 0b0000_1111; // expecting 1 - text message
            var lenByte = FirstLengthByte(buffer);

            ThrowIfMaskNotExpected(hasMask, expectMask);
            ThrowIfBadOpCode(opcode, finished, opCodeContinuation);

            var msglen = GetMessageLength(buffer, 0, lenByte);

            ThrowIfLengthZero(msglen);
            ThrowIfMsgLengthTooLong(msglen, maxLength);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ToggleMask(byte[] src, int sourceOffset, int messageLength, byte[] maskBuffer, int maskOffset)
        {
            ToggleMask(src, sourceOffset, src, sourceOffset, messageLength, maskBuffer, maskOffset);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ToggleMask(byte[] src, int sourceOffset, ArrayBuffer dst, int messageLength, byte[] maskBuffer, int maskOffset)
        {
            ToggleMask(src, sourceOffset, dst.array, 0, messageLength, maskBuffer, maskOffset);
            dst.count = messageLength;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ToggleMask(byte[] src, int srcOffset, byte[] dst, int dstOffset, int messageLength, byte[] maskBuffer, int maskOffset)
        {
            for (var i = 0; i < messageLength; i++)
            {
                var maskByte = maskBuffer[maskOffset + i % Constants.MaskSize];
                dst[dstOffset + i] = (byte)(src[srcOffset + i] ^ maskByte);
            }
        }

        /// <exception cref="InvalidDataException"></exception>
        private static int GetMessageLength(byte[] buffer, int offset, byte lenByte)
        {
            if (lenByte == Constants.UshortPayloadLength)
            {
                // header is 2 bytes
                ushort value = 0;
                value |= (ushort)(buffer[offset + 2] << 8);
                value |= buffer[offset + 3];

                return value;
            }

            if (lenByte == Constants.UlongPayloadLength)
            {
                // header is 8 bytes 
                ulong value = 0;
                value |= (ulong)buffer[offset + 2] << 56;
                value |= (ulong)buffer[offset + 3] << 48;
                value |= (ulong)buffer[offset + 4] << 40;
                value |= (ulong)buffer[offset + 5] << 32;
                value |= (ulong)buffer[offset + 6] << 24;
                value |= (ulong)buffer[offset + 7] << 16;
                value |= (ulong)buffer[offset + 8] << 8;
                value |= (ulong)buffer[offset + 9] << 0;

                if (value > int.MaxValue)
                    throw new NotSupportedException($"Can't receive payloads larger that int.max: {int.MaxValue}");

                return (int)value;
            }

            // is less than 126
            // header is 2 bytes long
            return lenByte;
        }

        /// <exception cref="InvalidDataException"></exception>
        private static void ThrowIfMaskNotExpected(bool hasMask, bool expectMask)
        {
            if (hasMask != expectMask)
                throw new InvalidDataException($"Message expected mask to be {expectMask} but was {hasMask}");
        }

        /// <exception cref="InvalidDataException"></exception>
        private static void ThrowIfBadOpCode(int opcode, bool finished, bool opCodeContinuation)
        {
            // 0 = continuation
            // 2 = binary
            // 8 = close

            // do we expect Continuation?
            if (opCodeContinuation)
            {
                // good it was Continuation
                if (opcode == 0)
                    return;

                // bad, wasn't Continuation
                throw new InvalidDataException("Expected opcode to be Continuation");
            }

            if (!finished)
            {
                // fragmented message, should be binary
                if (opcode == 2)
                    return;

                throw new InvalidDataException("Expected opcode to be binary");
            }

            // normal message, should be binary or close
            if (opcode == 2 || opcode == 8)
                return;

            throw new InvalidDataException("Expected opcode to be binary or close");
        }

        /// <exception cref="InvalidDataException"></exception>
        private static void ThrowIfLengthZero(int msglen)
        {
            if (msglen == 0)
                throw new InvalidDataException("Message length was zero");
        }

        /// <summary>
        ///     need to check this so that data from previous buffer isn't used
        /// </summary>
        public static void ThrowIfMsgLengthTooLong(int msglen, int maxLength)
        {
            if (msglen > maxLength)
                throw new InvalidDataException("Message length is greater than max length");
        }
    }
}