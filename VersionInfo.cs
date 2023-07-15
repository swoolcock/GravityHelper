// Copyright (c) Shane Woolcock. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;

namespace Celeste.Mod.GravityHelper;

public struct VersionInfo : IComparable<VersionInfo> {
    private int major;
    private int minor;
    private int build;
    private int revision;

    public int Major => major;
    public int Minor => minor;
    public int Build => build;
    public int Revision => revision;

    public short MajorRevision => (short) (revision >> 16);
    public short MinorRevision => (short) (revision % 0xFFFF);

    public VersionInfo(int major, int minor = 0, int build = -1, int revision = -1) {
        this.major = major;
        this.minor = minor;
        this.build = build;
        this.revision = revision;
    }

    public VersionInfo(string version) {
    }

    public int CompareTo(VersionInfo other) {
        if (major != other.major) {
            return major > other.major ? 1 : -1;
        }

        if (minor != other.minor) {
            return minor > other.minor ? 1 : -1;
        }

        if (build != other.build) {
            return build > other.build ? 1 : -1;
        }

        if (revision != other.revision) {
            return revision > other.revision ? 1 : -1;
        }

        return 0;
    }

    public bool Equals(VersionInfo other) =>
        major == other.major && minor == other.minor && build == other.build && revision == other.revision;

    public override bool Equals(object obj) =>
        obj is VersionInfo other && Equals(other);

    public override int GetHashCode() {
        int rv = 0;
        rv |= (major & 0x0000000F) << 28;
        rv |= (minor & 0x000000FF) << 20;
        rv |= (build & 0x000000FF) << 12;
        rv |= (revision & 0x00000FFF);
        return rv;
    }

    public override string ToString() => ToString(build < 0 ? 2 : revision < 0 ? 3 : 4);

    public string ToString(int fieldCount) {
        var str = string.Empty;
        if (fieldCount > 0) str = major.ToString();
        if (fieldCount > 1) str = $"{str}.{minor}";
        if (fieldCount > 2) str = $"{str}.{(build < 0 ? 0 : build)}";
        if (fieldCount > 3) str = $"{str}.{(revision < 0 ? 0 : revision)}";
        return str;
    }

    private static readonly char[] separators = { '.' };
    public static bool TryParse(string input, out VersionInfo result) {
        result = default;
        if (string.IsNullOrEmpty(input)) return false;

        var tokens = input.Split(separators);
        if (tokens.Length == 0) return false;
        if (!int.TryParse(tokens[0], out result.major)) return false;
        if (tokens.Length == 1) return true;
        if (!int.TryParse(tokens[1], out result.minor)) return false;
        if (tokens.Length == 2) return true;
        if (!int.TryParse(tokens[2], out result.build)) return false;
        if (tokens.Length == 3) return true;
        if (!int.TryParse(tokens[3], out result.revision)) return false;
        return tokens.Length == 4;
    }

    public VersionInfo Parse(string input, VersionInfo defaultValue = default) =>
        TryParse(input, out var result) ? result : defaultValue;

    public static bool operator ==(VersionInfo v1, VersionInfo v2) => v1.Equals(v2);
    public static bool operator !=(VersionInfo v1, VersionInfo v2) => !v1.Equals(v2);
    public static bool operator <(VersionInfo v1, VersionInfo v2) => v1.CompareTo(v2) < 0;
    public static bool operator >(VersionInfo v1, VersionInfo v2) => v1.CompareTo(v2) > 0;
    public static bool operator <=(VersionInfo v1, VersionInfo v2) => v1 == v2 || v1 < v2;
    public static bool operator >=(VersionInfo v1, VersionInfo v2) => v1 == v2 || v1 > v2;

    public static implicit operator VersionInfo(int major) => new VersionInfo(major);
}
