using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace SM64AppBase
{
    class MagicManager
    {
        const uint PageSize = 4096;
        const long MaxMem = (long)2 * 1024 * 1024 * 1024;

        const uint ramMagic = 0x3C1A8032;
        const uint romMagic = 0x80371240;

        private Process process;
        public readonly int romPtrBase;
        public readonly int ramPtrBase;

        public MagicManager(Process process, int[] romPtrBaseSuggestions, int[] ramPtrBaseSuggestions)
        {
            this.process = process;

            bool isRomFound = false;
            bool isRamFound = false;

            foreach (int romPtrBaseSuggestion in romPtrBaseSuggestions)
            {
                romPtrBase = romPtrBaseSuggestion;
                if (IsRomBaseValid())
                {
                    isRomFound = true;
                    break;
                }
            }

            foreach (int ramPtrBaseSuggestion in ramPtrBaseSuggestions)
            {
                ramPtrBase = ramPtrBaseSuggestion;
                if (IsRamBaseValid())
                {
                    isRamFound = true;
                    break;
                }
            }

            for (uint a = 0; a < MaxMem; a += PageSize)
            {
                IntPtr addr = (IntPtr)a;
                if (isRomFound && isRamFound)
                    break;

                uint value = 0;
                bool readSuccess = ProcessReader.ReadUInt32(process, addr, out value);

                if (readSuccess)
                {
                    if (!isRamFound && value == ramMagic)
                    {
                        ramPtrBase = addr.ToInt32();
                        isRamFound = true;
                    }

                    if (!isRomFound && value == romMagic)
                    {
                        romPtrBase = addr.ToInt32();
                        isRomFound = true;
                    }
                }
                else
                    break;
            }
        }

        bool IsRamBaseValid()
        {
            uint value = 0;
            bool readSuccess = ProcessReader.ReadUInt32( process, new IntPtr(ramPtrBase), out value);
            return readSuccess && (value == ramMagic);
        }

        bool IsRomBaseValid()
        {
            uint value = 0;
            bool readSuccess = ProcessReader.ReadUInt32(process, new IntPtr(romPtrBase), out value);
            return readSuccess && (value == romMagic);
        }

        public bool isValid()
        {
            return IsRamBaseValid() && IsRomBaseValid();
        }
    }
}
