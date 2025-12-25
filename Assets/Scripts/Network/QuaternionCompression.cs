using UnityEngine;

namespace TacticalCombat.Network
{
    /// <summary>
    /// ✅ QUATERNION COMPRESSION (MirrorUnityFPS Pattern)
    /// Smallest-three compression: 16 bytes → 4 bytes (75% reduction)
    /// Used for network rotation sync to reduce bandwidth
    /// </summary>
    public static class QuaternionCompression
    {
        // Compression precision (10 bits per component = 1024 values)
        private const int BITS_PER_COMPONENT = 10;
        private const int MAX_VALUE = (1 << BITS_PER_COMPONENT) - 1; // 1023
        private const float RANGE = 1.414214f; // sqrt(2) - max value for 3 components

        /// <summary>
        /// Compress quaternion to 32 bits (4 bytes)
        /// Uses smallest-three algorithm:
        /// - Find largest component (2 bits to identify which one)
        /// - Store other 3 components (10 bits each)
        /// - Reconstruct largest from w² + x² + y² + z² = 1
        /// </summary>
        public static uint Compress(Quaternion rotation)
        {
            // Ensure quaternion is normalized
            rotation = Quaternion.Normalize(rotation);

            // Find largest component index (0=w, 1=x, 2=y, 3=z)
            float maxValue = float.MinValue;
            int maxIndex = 0;

            float absW = Mathf.Abs(rotation.w);
            float absX = Mathf.Abs(rotation.x);
            float absY = Mathf.Abs(rotation.y);
            float absZ = Mathf.Abs(rotation.z);

            if (absW > maxValue) { maxValue = absW; maxIndex = 0; }
            if (absX > maxValue) { maxValue = absX; maxIndex = 1; }
            if (absY > maxValue) { maxValue = absY; maxIndex = 2; }
            if (absZ > maxValue) { maxValue = absZ; maxIndex = 3; }

            // Determine sign of largest component (we'll restore this during decompression)
            float sign = 1.0f;
            switch (maxIndex)
            {
                case 0: sign = rotation.w >= 0 ? 1.0f : -1.0f; break;
                case 1: sign = rotation.x >= 0 ? 1.0f : -1.0f; break;
                case 2: sign = rotation.y >= 0 ? 1.0f : -1.0f; break;
                case 3: sign = rotation.z >= 0 ? 1.0f : -1.0f; break;
            }

            // Get the other 3 components and scale them
            float a = 0, b = 0, c = 0;
            switch (maxIndex)
            {
                case 0: // largest is w
                    a = rotation.x * sign;
                    b = rotation.y * sign;
                    c = rotation.z * sign;
                    break;
                case 1: // largest is x
                    a = rotation.w * sign;
                    b = rotation.y * sign;
                    c = rotation.z * sign;
                    break;
                case 2: // largest is y
                    a = rotation.w * sign;
                    b = rotation.x * sign;
                    c = rotation.z * sign;
                    break;
                case 3: // largest is z
                    a = rotation.w * sign;
                    b = rotation.x * sign;
                    c = rotation.y * sign;
                    break;
            }

            // Scale from [-1/√2, 1/√2] to [0, 1023]
            int quantizedA = Mathf.RoundToInt((a + RANGE) / (2.0f * RANGE) * MAX_VALUE);
            int quantizedB = Mathf.RoundToInt((b + RANGE) / (2.0f * RANGE) * MAX_VALUE);
            int quantizedC = Mathf.RoundToInt((c + RANGE) / (2.0f * RANGE) * MAX_VALUE);

            // Clamp to valid range
            quantizedA = Mathf.Clamp(quantizedA, 0, MAX_VALUE);
            quantizedB = Mathf.Clamp(quantizedB, 0, MAX_VALUE);
            quantizedC = Mathf.Clamp(quantizedC, 0, MAX_VALUE);

            // Pack into 32 bits:
            // 2 bits: maxIndex (which component is largest)
            // 10 bits: component A
            // 10 bits: component B
            // 10 bits: component C
            uint compressed = 0;
            compressed |= (uint)maxIndex << 30;           // Bits 30-31: max index
            compressed |= (uint)quantizedA << 20;         // Bits 20-29: component A
            compressed |= (uint)quantizedB << 10;         // Bits 10-19: component B
            compressed |= (uint)quantizedC;               // Bits 0-9:   component C

            return compressed;
        }

        /// <summary>
        /// Decompress 32-bit value back to quaternion
        /// </summary>
        public static Quaternion Decompress(uint compressed)
        {
            // Extract packed components
            int maxIndex = (int)((compressed >> 30) & 0x3);        // 2 bits
            int quantizedA = (int)((compressed >> 20) & 0x3FF);    // 10 bits
            int quantizedB = (int)((compressed >> 10) & 0x3FF);    // 10 bits
            int quantizedC = (int)(compressed & 0x3FF);            // 10 bits

            // Unscale from [0, 1023] to [-1/√2, 1/√2]
            float a = (quantizedA / (float)MAX_VALUE) * (2.0f * RANGE) - RANGE;
            float b = (quantizedB / (float)MAX_VALUE) * (2.0f * RANGE) - RANGE;
            float c = (quantizedC / (float)MAX_VALUE) * (2.0f * RANGE) - RANGE;

            // Calculate largest component using w² + x² + y² + z² = 1
            float sumSquares = a * a + b * b + c * c;
            float largestValue = Mathf.Sqrt(Mathf.Max(0, 1.0f - sumSquares));

            // Reconstruct quaternion
            Quaternion result = Quaternion.identity;
            switch (maxIndex)
            {
                case 0: // largest was w
                    result.w = largestValue;
                    result.x = a;
                    result.y = b;
                    result.z = c;
                    break;
                case 1: // largest was x
                    result.w = a;
                    result.x = largestValue;
                    result.y = b;
                    result.z = c;
                    break;
                case 2: // largest was y
                    result.w = a;
                    result.x = b;
                    result.y = largestValue;
                    result.z = c;
                    break;
                case 3: // largest was z
                    result.w = a;
                    result.x = b;
                    result.y = c;
                    result.z = largestValue;
                    break;
            }

            // Normalize to ensure valid quaternion
            return Quaternion.Normalize(result);
        }

        /// <summary>
        /// Calculate compression error (for debugging)
        /// </summary>
        public static float GetCompressionError(Quaternion original)
        {
            uint compressed = Compress(original);
            Quaternion decompressed = Decompress(compressed);
            return Quaternion.Angle(original, decompressed);
        }
    }
}
