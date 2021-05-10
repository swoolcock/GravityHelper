// Copyright (c) Shane Woolcock. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

namespace GravityHelper.Entities
{
    public class ConnectedFieldGroup
    {
        private static int nextId;
        public int ID = nextId++;
        public float Flash;

        public bool Flashing => Flash
    }
}
