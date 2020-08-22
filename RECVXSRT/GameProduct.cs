using System;

namespace RECVXSRT
{
    public class GameProduct
    {
        public string Code { get; private set; }
        public string Name { get; private set; }
        public string Country { get; private set; }
        public string System { get; private set; }
        public bool Supported { get; private set; }

        public const string SLPM_65022 = "SLPM_650.22";
        public const string SLUS_20184 = "SLUS_201.84";
        public const string SLES_50306 = "SLES_503.06";
        public const string NPJB00135 = "NPJB00135";
        public const string NPUB30467 = "NPUB30467";
        public const string NPEB00553 = "NPEB00553";

        public GameProduct()
        {
            SetCode();
        }

        public GameProduct(string code)
        {
            SetCode(code);
        }

        public void SetCode(string code = null)
        {
            Code = code;
            Supported = true;
            System = String.Empty;

            switch (Code)
            {
                case SLPM_65022:
                    Name = "BioHazard Code: Veronica Kanzenban";
                    Country = "JP";
                    System = "PS2";
                    break;

                case SLUS_20184:
                    Name = "Resident Evil Code: Veronica X";
                    Country = "US";
                    System = "PS2";
                    break;

                case SLES_50306:
                    Name = "Resident Evil Code: Veronica X";
                    Country = "EU";
                    System = "PS2";
                    break;

                case NPJB00135:
                    Name = "BioHazard Code: Veronica Kanzenban";
                    Country = "JP";
                    System = "PS3";
                    break;

                case NPUB30467:
                    Name = "Resident Evil Code: Veronica X HD";
                    Country = "US";
                    System = "PS3";
                    break;

                case NPEB00553:
                    Name = "Resident Evil Code: Veronica X";
                    Country = "EU";
                    System = "PS3";
                    break;

                default:
                    Name = "Unsupported Game";
                    Country = "None";
                    Supported = false;
                    break;
            }
        }
    }
}