using BlitzCommon.Blitz.Cmn.Def;
using BlitzCommon.Resources.Source;
using Nintendo;
using System.Collections.Generic;
using System.IO;

namespace BlitzSniffer.Game.Resources
{
    class GearResource
    {
        private static GearResource _Instance;

        public static GearResource Instance
        {
            get
            {
                if (_Instance == null)
                {
                    _Instance = new GearResource();
                }

                return _Instance;
            }
        }

        private Dictionary<int, string> Headgear = new Dictionary<int, string>();
        private Dictionary<int, string> Clothes = new Dictionary<int, string>();
        private Dictionary<int, string> Shoes = new Dictionary<int, string>();

        private GearResource()
        {
            LoadInfo("Head", Headgear);
            LoadInfo("Clothes", Clothes);
            LoadInfo("Shoes", Shoes);
        }

        private void LoadInfo(string bymlName, Dictionary<int, string> targetDict)
        {
            using (Stream infoStream = GameResourceSource.Instance.GetFile($"/Mush/GearInfo_{bymlName}.release.byml"))
            {
                dynamic infoByml = ByamlLoader.LoadByamlDynamic(infoStream);

                foreach (Dictionary<string, dynamic> info in infoByml)
                {
                    targetDict.Add(info["Id"], info["Name"]);
                }
            }
        }

        public string GetGear(GearKind kind, int id)
        {
            switch (kind)
            {
                case GearKind.Head:
                    return Headgear[id];
                case GearKind.Clothes:
                    return Clothes[id];
                case GearKind.Shoes:
                    return Shoes[id];
                default:
                    return $"Unk{id}";
            }
        }

    }
}
