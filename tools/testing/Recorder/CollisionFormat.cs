namespace ArtOfSimRally.Testing
{
    internal static class CollisionFormat
    {
        public const int Capacity = 4096, ContactLimit = 8;
        public const string Header = "event,time_s,physics_time_s,epoch,last_force_row,body_id,other_id,other_layer,road,crowd,contacts,examined,selected,rvx_mps,rvy_mps,rvz_mps,ix_ns,iy_ns,iz_ns,mass_kg,px_m,py_m,pz_m,qx,qy,qz,qw,vx_mps,vy_mps,vz_mps,nx,ny,nz,cpx_m,cpy_m,cpz_m";
    }
}
