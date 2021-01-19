using BlitzCommon.Resources;
using BlitzSniffer.Config;
using SKM.V3;
using SKM.V3.Methods;
using SKM.V3.Models;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace BlitzSniffer.Util
{
    class LicenseTools
    {
        public static readonly string CRYPTOLENS_PUBLIC_KEY = "<RSAKeyValue><Modulus>kXe0NP7Dco5g85KOziWQT+oK21VkKwp+4XeR6GOTf46u2F3UwdFK3UYA1wXxIobbWoCpvX+7Yq/gGlV03IEqjzfxePwMXKd31EIFT7fez/hKz29YRD6A9pIJwqnHfJo8Xfje/6vxj83nvlvLXLgLutJs4tKK+hM43EAKy2NEs3mF/qeu88tPX3MMkrqrN0N2/I2tPnUgiMjV/pZ02wWhZSFnsfxhpcmwUI0mTYPcYa8317oG2BoXtNiS7wpurHygZPPRpcqc/BJjR7117N3IY7GIBa7qsBhcyzjr86m+Wt2s65kt3A5vI9jAjQ7cTIPIhzvWJCoeVOwTdjJSpjZsxw==</Modulus><Exponent>AQAB</Exponent></RSAKeyValue>";
        private static readonly string CRYPTOLENS_AUTH_TOKEN = "WyIyNTg2NDgiLCJueitsRVVmQWFpVWIwVHM5RzdtTjJkNjkxekxHR0czb2ROU2phNEVyIl0=";

        private static LicenseTools _Instance;

        public static LicenseTools Instance
        {
            get
            {
                if (_Instance == null)
                {
                    _Instance = new LicenseTools();
                }

                return _Instance;
            }
        }

        private LicenseKey LocalLicense;

        public bool LoadAndVerifyLicense()
        {
            LocalLicense = GetLicenseByInet();
            if (LocalLicense == null || !LocalLicense.HasValidSignature(CRYPTOLENS_PUBLIC_KEY).IsValid())
            {
                LocalLicense = null;

                return false;
            }

            return true;
        }

        public LicenseKey GetLoadedLicense()
        {
            return LocalLicense;
        }

        private LicenseKey GetLicenseByInet()
        {
            KeyInfoResult keyResult = Key.Activate(token: CRYPTOLENS_AUTH_TOKEN, parameters: new ActivateModel()
            {
                Key = SnifferConfig.Instance.Key,
                ProductId = 8988,
                Sign = true,
                MachineCode = Helpers.GetMachineCodePI()
            });

            if (keyResult == null || keyResult.Result == ResultType.Error)
            {
                return null;
            }

            return keyResult.LicenseKey;
        }

    }
}
