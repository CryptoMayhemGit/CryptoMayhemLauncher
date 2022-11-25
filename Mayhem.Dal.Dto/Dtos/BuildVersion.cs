namespace Mayhem.Dal.Dto.Dtos
{
    public struct BuildVersion
    {
        public static BuildVersion zero = new BuildVersion(0, 0, 0);

        private short major;
        private short minor;
        private short subMinor;

        public BuildVersion(short _major, short _minor, short _subMinor)
        {
            major = _major;
            minor = _minor;
            subMinor = _subMinor;
        }

        public BuildVersion(string _version)
        {
            string[] versionStrings = _version.Split('.');
            if (versionStrings.Length != 3)
            {
                major = 0;
                minor = 0;
                subMinor = 0;
                return;
            }

            major = short.Parse(versionStrings[0]);
            minor = short.Parse(versionStrings[1]);
            subMinor = short.Parse(versionStrings[2]);
        }

        public bool IsDifferentThan(BuildVersion otherVersion)
        {
            if (major != otherVersion.major || minor != otherVersion.minor || subMinor != otherVersion.subMinor)
            {
                return true;
            }

            return false;
        }

        public bool IsEquals(BuildVersion otherVersion)
        {
            if (major == otherVersion.major && minor == otherVersion.minor && subMinor == otherVersion.subMinor)
            {
                return true;
            }

            return false;
        }

        public override string ToString()
        {
            return $"{major}.{minor}.{subMinor}";
        }
    }
}
