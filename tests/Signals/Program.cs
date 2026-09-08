using System.Text.Json;
using ArtOfSimRally.Mod;
using UnityEngine;
using Dbce.Wheel.Telemetry;
using System.Net;
using System.Net.Sockets;

static class Program
{
    static int assertions;
    static void Check(bool ok, string message) { assertions++; if (!ok) throw new Exception(message); }
    static void Near(float actual, float expected, string message) => Check(Math.Abs(actual-expected)<0.002f, message+": "+actual);
    static void Vector(Vector3 value, float x, float y, float z, string message)
    { Near(value.x,x,message+" x"); Near(value.y,y,message+" y"); Near(value.z,z,message+" z"); }
    static void Travel()
    {
        Near(TelemetrySampling.TravelMeters(.1f,.2f),.1f,"half travel meters");
        Near(TelemetrySampling.TravelNormalized(.1f,.2f),.5f,"half travel ratio");
        foreach(float capacity in new[]{.1f,.2f,.35f,.6f})
            foreach(float fraction in new[]{0f,.25f,.5f,1f,2f,-1f})
            {
                float bounded=Math.Clamp(fraction,0,1);
                Near(TelemetrySampling.TravelMeters(capacity*fraction,capacity),capacity*bounded,"travel clamped");
                Near(TelemetrySampling.TravelNormalized(capacity*fraction,capacity),bounded,"ratio independent of capacity");
            }
        foreach(float bad in new[]{0f,-1f,float.NaN,float.PositiveInfinity,float.NegativeInfinity})
        { Near(TelemetrySampling.TravelNormalized(.1f,bad),0,"bad capacity"); Near(TelemetrySampling.TravelMeters(.1f,bad),0,"bad capacity meters"); }
        foreach(float bad in new[]{float.NaN,float.PositiveInfinity,float.NegativeInfinity})
        { Near(TelemetrySampling.TravelNormalized(bad,.2f),0,"bad compression"); Near(TelemetrySampling.TravelMeters(bad,.2f),0,"bad compression meters"); }
    }
    static void Projection()
    {
        var value=new Vector3(1,2,3);
        Vector(TelemetrySampling.LocalVector(value,new Quaternion(0,0,0,1)),1,2,3,"identity");
        float half=(float)Math.Sqrt(.5);
        Vector(TelemetrySampling.LocalVector(value,new Quaternion(0,half,0,half)),-3,2,1,"yaw right");
        Vector(TelemetrySampling.LocalVector(value,new Quaternion(half,0,0,half)),1,3,-2,"pitch");
        Vector(TelemetrySampling.LocalVector(value,new Quaternion(0,0,half,half)),2,-1,3,"roll");
        Vector(TelemetrySampling.LocalVector(value,new Quaternion(0,2*half,0,2*half)),-3,2,1,"quaternion normalization");
        for(int i=0;i<360;i++)
        {
            double radians=i*Math.PI/180;
            var rotation=new Quaternion(0,(float)Math.Sin(radians/2),0,(float)Math.Cos(radians/2));
            var worldForward=new Vector3((float)(20*Math.Sin(radians)),0,(float)(20*Math.Cos(radians)));
            Vector(TelemetrySampling.LocalVector(worldForward,rotation),0,0,20,"forward at every heading");
        }
        Vector(TelemetrySampling.LocalVector(value,new Quaternion()),0,0,0,"invalid zero rotation");
        Vector(TelemetrySampling.LocalVector(value,new Quaternion(float.NaN,0,0,1)),0,0,0,"invalid rotation");
        Vector(TelemetrySampling.LocalVector(new Vector3(float.NaN,0,0),new Quaternion(0,0,0,1)),0,0,0,"invalid vector");
    }
    static void Motion()
    {
        var history=new TelemetryMotion();
        Vector(history.Acceleration(new Vector3(),new Vector3(20,0,0),1),0,0,0,"spawn moving");
        Vector(history.Acceleration(new Vector3(2,0,0),new Vector3(21,0,0),1.1f),10,0,0,"acceleration");
        Vector(history.Acceleration(new Vector3(4.1f,0,0),new Vector3(21,0,0),1.2f),0,0,0,"constant speed");
        Vector(history.Acceleration(new Vector3(4.2f,0,0),new Vector3(1,0,0),1.3f),-200,0,0,"real collision preserved");
        Vector(history.Acceleration(new Vector3(100,0,0),new Vector3(50,0,0),1.4f),0,0,0,"teleport");
        Vector(history.Acceleration(new Vector3(105,0,0),new Vector3(50,0,0),1.5f),0,0,0,"post teleport baseline");
        Vector(history.Acceleration(new Vector3(106,0,0),new Vector3(10,0,0),2),0,0,0,"long gap");
        Vector(history.Acceleration(new Vector3(106,0,0),new Vector3(30,0,0),2),0,0,0,"duplicate clock");
        Vector(history.Acceleration(new Vector3(),new Vector3(50,0,0),0),0,0,0,"clock rollback");
        history.Reset();
        Vector(history.Acceleration(new Vector3(),new Vector3(50,0,0),.1f),0,0,0,"pause/restart reset");
        Vector(history.Acceleration(new Vector3(float.NaN,0,0),new Vector3(),.2f),0,0,0,"invalid position");
        Vector(history.Acceleration(new Vector3(),new Vector3(60,0,0),.3f),0,0,0,"invalid sample drops baseline");
        Vector(history.Acceleration(new Vector3(),new Vector3(float.PositiveInfinity,0,0),.4f),0,0,0,"invalid velocity");
        Vector(history.Acceleration(new Vector3(),new Vector3(60,0,0),float.NaN),0,0,0,"invalid clock");
        history.Reset(); history.Acceleration(new Vector3(),new Vector3(10,0,0),1);
        var acceleration=history.Acceleration(new Vector3(1,0,0),new Vector3(10,0,0),1.1f);
        float half=(float)Math.Sqrt(.5);
        Vector(TelemetrySampling.LocalVector(acceleration,new Quaternion(0,half,0,half)),0,0,0,"rotating frame adds no acceleration");
    }
    static int Main()
    {
        try { Travel(); Projection(); Motion(); PacketLoopback(); Console.WriteLine(JsonSerializer.Serialize(new{status="passed",assertions,scope="actual sampling math and encoded UDP; no Unity drive or motion hardware"})); return 0; }
        catch(Exception ex) { Console.Error.WriteLine(ex); return 1; }
    }
    static void PacketLoopback()
    {
        var frame=new TelemetryFrame { IsRaceOn=true };
        TelemetrySampling.FillSuspension(ref frame,new WheelValues(.1f,.05f,.3f,.2f),new WheelValues(.2f,.2f,.4f,.2f));
        float half=(float)Math.Sqrt(.5);
        TelemetrySampling.FillMotion(ref frame,new Vector3(20,0,0),new Vector3(2,0,0),new Vector3(1,2,3),new Quaternion(0,half,0,half));
        using var listener=new UdpClient(new IPEndPoint(IPAddress.Loopback,0));
        listener.Client.ReceiveTimeout=2000;
        using var sender=new TelemetrySender("127.0.0.1",((IPEndPoint)listener.Client.LocalEndPoint).Port);
        sender.Send(frame); IPEndPoint peer=null; byte[] packet=listener.Receive(ref peer);
        Check(packet.Length==ForzaPacket.Size && packet[323]==(byte)'R',"encoded packet identity");
        var meters=new[]{.1f,.05f,.3f,.2f}; var ratios=new[]{.5f,.25f,.75f,1f};
        for(int i=0;i<4;i++)
        {
            Near(BitConverter.ToSingle(packet,ForzaPacket.OffSuspensionTravelMeters+i*4),meters[i],"encoded corner meters");
            Near(BitConverter.ToSingle(packet,ForzaPacket.OffNormalizedSuspensionTravel+i*4),ratios[i],"encoded corner compression");
        }
        Near(BitConverter.ToSingle(packet,ForzaPacket.OffVelocityX),0,"encoded local lateral velocity");
        Near(BitConverter.ToSingle(packet,ForzaPacket.OffVelocityX+8),20,"encoded local forward velocity");
        Near(BitConverter.ToSingle(packet,ForzaPacket.OffAccelerationX+8),2,"encoded local acceleration");
        Near(BitConverter.ToSingle(packet,ForzaPacket.OffAngularVelocityX),-3,"encoded local angular x");
        Near(BitConverter.ToSingle(packet,ForzaPacket.OffAngularVelocityX+4),2,"encoded local angular y");
        Near(BitConverter.ToSingle(packet,ForzaPacket.OffAngularVelocityX+8),1,"encoded local angular z");
    }
}
