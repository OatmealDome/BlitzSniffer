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

        private static readonly string CACHED_LICENSE_FILE = "CachedLicense.bsrsrc";
        private static readonly byte[] CACHED_LICENSE_FILE_KEY = new byte[] { 0xed, 0x3d, 0xdd, 0xdc, 0x41, 0x62, 0xb9, 0x0e, 0xdb, 0x49, 0x89, 0x42, 0xe6, 0x64, 0xfc, 0x96 };

        private static readonly int MAX_OFFLINE_DAYS = 5;

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
            try
            {
                LocalLicense = GetLicenseByFile();
                if (LocalLicense == null || !LocalLicense.HasValidSignature(CRYPTOLENS_PUBLIC_KEY, MAX_OFFLINE_DAYS).IsValid())
                {
                    LocalLicense = GetLicenseByInet();
                    if (LocalLicense == null || !LocalLicense.HasValidSignature(CRYPTOLENS_PUBLIC_KEY).IsValid())
                    {
                        LocalLicense = null;

                        return false;
                    }

                    // Save to file for caching
                    string licenseStr = LocalLicense.SaveAsString();
                    SnifferResource resource = new SnifferResource(Encoding.UTF8.GetBytes(licenseStr), CACHED_LICENSE_FILE_KEY);

                    File.WriteAllBytes(CACHED_LICENSE_FILE, resource.Serialize());
                }
            }
            catch (Exception)
            {
                return false;
            }

            return true;
        }

        public LicenseKey GetLoadedLicense()
        {
            return LocalLicense;
        }

        public string GetDataObjectString(string name)
        {
            Trace.Assert(LocalLicense != null);

            DataObject dataObject = LocalLicense.DataObjects.Where(d => d.Name == name).FirstOrDefault();

            if (dataObject == null)
            {
                return null;
            }

            return dataObject.StringValue;
        }

        private LicenseKey GetLicenseByInet()
        {
            KeyInfoResult keyResult = Key.Activate(token: CRYPTOLENS_AUTH_TOKEN, parameters: new ActivateModel()
            {
                Key = SnifferConfig.Instance.Key,
                ProductId = 8988,
                Sign = true
            });

            if (keyResult == null || keyResult.Result == ResultType.Error)
            {
                return null;
            }

            return keyResult.LicenseKey;
        }

        private LicenseKey GetLicenseByFile()
        {
            if (!File.Exists(CACHED_LICENSE_FILE))
            {
                return null;
            }

            try
            {
                SnifferResource resource = SnifferResource.Deserialize(File.ReadAllBytes(CACHED_LICENSE_FILE), CACHED_LICENSE_FILE_KEY);
                string licenseStr = Encoding.UTF8.GetString(resource.Data);

                return new LicenseKey().LoadFromString(CRYPTOLENS_PUBLIC_KEY, licenseStr);
            }
            catch (Exception)
            {
                return null;
            }
        }

    }
}
