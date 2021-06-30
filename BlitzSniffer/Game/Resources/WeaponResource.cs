using BlitzCommon.Resources.Source;
using Nintendo;
using System.Collections.Generic;
using System.IO;

namespace BlitzSniffer.Game.Resources
{
    class WeaponResource
    {
        private static WeaponResource _Instance;

        public static WeaponResource Instance
        {
            get
            {
                if (_Instance == null)
                {
                    _Instance = new WeaponResource();
                }

                return _Instance;
            }
        }

        private Dictionary<int, string> MainWeapons = new Dictionary<int, string>();
        private Dictionary<int, string> SubWeapons = new Dictionary<int, string>();
        private Dictionary<int, string> SpecialWeapons = new Dictionary<int, string>();

        private WeaponResource()
        {
            LoadInfo("Main.release", "Wst", MainWeapons);
            LoadInfo("Sub", "Wsb", SubWeapons);
            LoadInfo("Special", "Wsp", SpecialWeapons);
        }

        private void LoadInfo(string bymlName, string namePrefix, Dictionary<int, string> targetDict)
        {
            using (Stream infoStream = GameResourceSource.Instance.GetFile($"/Mush/WeaponInfo_{bymlName}.byml"))
            {
                dynamic infoByml = ByamlLoader.LoadByamlDynamic(infoStream);

                foreach (Dictionary<string, dynamic> info in infoByml)
                {
                    targetDict.Add(info["Id"], $"{namePrefix}_{info["Name"]}");
                }
            }
        }

        public string GetMainWeapon(int id)
        {
            return MainWeapons[id];
        }

        public string GetSubWeapon(int id)
        {
            return SubWeapons[id];
        }

        public string GetSpecialWeapon(int id)
        {
            return SpecialWeapons[id];
        }

    }
}
