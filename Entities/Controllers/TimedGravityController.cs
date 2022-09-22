// Copyright (c) Shane Woolcock. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Linq;
using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.GravityHelper.Entities.Controllers
{
    [CustomEntity("GravityHelper/TimedGravityController")]
    [Tracked]
    public class TimedGravityController : BaseGravityController<TimedGravityController>
    {
        public TimedPair[] TimedSequence { get; }
        public GravityType[] CassetteSequence { get; }
        public bool SyncIndicators { get; }

        // ReSharper disable once UnusedMember.Global
        public TimedGravityController()
        {
            // ephemeral controller
        }

        // ReSharper disable once UnusedMember.Global
        public TimedGravityController(EntityData data, Vector2 offset)
            : base(data, offset)
        {
            // timed sequence is a pipe separated list of comma separated pairs
            // type,delay|type,delay|type,delay
            var timedSequenceString = data.Attr("timedSequence");
            TimedSequence = timedSequenceString
                .Split('|')
                .Select(s => TimedPair.TryParse(s, out var pair) ? pair : (TimedPair?)null)
                .OfType<TimedPair>()
                .ToArray();

            // cassette sequence is a pipe or comma separated list
            // type,type,type,type
            var cassetteSequenceString = data.Attr("cassetteSequence").Replace("|", ",");
            CassetteSequence = cassetteSequenceString
                .Split(',')
                .Select(s =>
                {
                    if (!int.TryParse(s, out var value)) return default;
                    var type = (GravityType)value;
                    return type >= GravityType.None && type <= GravityType.Toggle ? type : default;
                })
                .ToArray();

            SyncIndicators = data.Bool("syncIndicators", true);
        }
    }

    public struct TimedPair
    {
        public GravityType Type;
        public float Delay;

        public static bool TryParse(string str, out TimedPair pair)
        {
            var tokens = (str ?? string.Empty).Split(',');
            pair = default;

            // must be two tokens
            if (tokens.Length != 2) return false;

            // first token must be a valid gravity type
            if (!int.TryParse(tokens[0], out var typeInt)) return false;
            var type = (GravityType)typeInt;
            if (type < GravityType.None || type > GravityType.Toggle) return false;

            // second token must be a non-negative delay
            if (!float.TryParse(tokens[1], out var delay)) return false;
            if (delay < 0) return false;

            // create a new pair!
            pair = new TimedPair { Type = type, Delay = delay };
            return true;
        }
    }
}
