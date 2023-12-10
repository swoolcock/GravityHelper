// Copyright (c) Shane Woolcock. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using Celeste.Mod.CelesteNet;
using Celeste.Mod.CelesteNet.DataTypes;

namespace Celeste.Mod.GravityHelper.ThirdParty.CelesteNet;

internal class DataPlayerGravity : DataType<DataPlayerGravity>
{
    static DataPlayerGravity()
    {
        DataID = $"GravityHelper_PlayerGravity";
    }

    public override DataFlags DataFlags => DataFlags.CoreType;

    public DataPlayerInfo Player;

    public GravityType GravityType = GravityType.Normal;

    // Can be RECEIVED BY CLIENT TOO EARLY because UDP is UDP.
    public override bool FilterHandle(DataContext ctx) => Player != null;

    public override MetaType[] GenerateMeta(DataContext ctx) => new MetaType[]
    {
        new MetaPlayerPrivateState(Player),
        new MetaBoundRef(DataType<DataPlayerInfo>.DataID, Player?.ID ?? uint.MaxValue, true),
    };

    public override void FixupMeta(DataContext ctx)
    {
        Player = Get<MetaPlayerPrivateState>(ctx);
        Get<MetaBoundRef>(ctx).ID = Player?.ID ?? uint.MaxValue;
    }

    protected override void Read(CelesteNetBinaryReader reader)
    {
        var protocolVersion = reader.ReadInt32();
        GravityType = (GravityType)reader.ReadInt32();
    }

    protected override void Write(CelesteNetBinaryWriter writer)
    {
        writer.Write(CelesteNetModSupport.PROTOCOL_VERSION);
        writer.Write((int)GravityType);
    }
}