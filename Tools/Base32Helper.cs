using System;
using System.Collections.Generic;
using System.Text;

namespace MedicalReportsApp.Tools
{
    public static class Base32Helper
    {
        private const string Alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";

        public static string Encode(byte[] data)
        {
            if (data == null || data.Length == 0)
            {
                return "";
            }

            StringBuilder result = new StringBuilder();
            int buffer = data[0];
            int next = 1;
            int bitsLeft = 8;

            while (bitsLeft > 0 || next < data.Length)
            {
                if (bitsLeft < 5)
                {
                    if (next < data.Length)
                    {
                        buffer <<= 8;
                        buffer |= data[next++] & 255;
                        bitsLeft += 8;
                    }
                    else
                    {
                        int pad = 5 - bitsLeft;
                        buffer <<= pad;
                        bitsLeft += pad;
                    }
                }

                int index = (buffer >> (bitsLeft - 5)) & 31;
                bitsLeft -= 5;
                result.Append(Alphabet[index]);
            }

            return result.ToString();
        }

        public static byte[] Decode(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
            {
                return new byte[0];
            }

            string value = input.Trim().TrimEnd('=').Replace(" ", "").ToUpperInvariant();
            List<byte> bytes = new List<byte>();
            int buffer = 0;
            int bitsLeft = 0;

            foreach (char c in value)
            {
                int index = Alphabet.IndexOf(c);

                if (index < 0)
                {
                    throw new FormatException("Invalid Base32 string.");
                }

                buffer <<= 5;
                buffer |= index & 31;
                bitsLeft += 5;

                if (bitsLeft >= 8)
                {
                    bytes.Add((byte)((buffer >> (bitsLeft - 8)) & 255));
                    bitsLeft -= 8;
                }
            }

            return bytes.ToArray();
        }
    }
}
