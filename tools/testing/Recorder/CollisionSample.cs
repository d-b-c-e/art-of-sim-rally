using System.Globalization;
using System.IO;

namespace ArtOfSimRally.Testing
{
    // Values only. One observation per collision entry, not per physics frame.
    internal struct CollisionSample
    {
        public int Epoch, LastForceRow, BodyId, OtherId, OtherLayer, Road, Crowd, Contacts, Examined, Selected;
        public float Time, PhysicsTime, Rvx, Rvy, Rvz, Ix, Iy, Iz, Mass;
        public float Px, Py, Pz, Qx, Qy, Qz, Qw, Vx, Vy, Vz, Nx, Ny, Nz, Cpx, Cpy, Cpz;
        // Serialization is called only during STOP, never in a Unity callback.
        public void Write(TextWriter writer, int index)
        {
            writer.WriteLine(string.Join(",", index, F(Time), F(PhysicsTime), Epoch, LastForceRow, BodyId, OtherId,
                OtherLayer, Road, Crowd, Contacts, Examined, Selected, F(Rvx), F(Rvy), F(Rvz), F(Ix), F(Iy), F(Iz),
                F(Mass), F(Px), F(Py), F(Pz), F(Qx), F(Qy), F(Qz), F(Qw), F(Vx), F(Vy), F(Vz),
                F(Nx), F(Ny), F(Nz), F(Cpx), F(Cpy), F(Cpz)));
        }
        private static string F(float value) => value.ToString("R", CultureInfo.InvariantCulture);
    }
}
