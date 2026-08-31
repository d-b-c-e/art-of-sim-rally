using System;
using ArtOfSimRally.Telemetry;
using Xunit;

namespace ArtOfSimRally.Telemetry.Tests
{
    /// <summary>
    /// Locks down the Forza Horizon 4/5 Data Out wire format.
    /// </summary>
    /// <remarks>
    /// These are regression tests against silent breakage. A wrong offset does not
    /// throw — it produces a packet a dashboard happily renders with the wrong
    /// numbers in it, which is far more expensive to notice. The two anchor offsets
    /// (Speed at 256, Gear at 319) were validated live against SimHub's Forza
    /// Horizon profile by the cruisn-collection harness; if a change breaks those,
    /// the change is wrong.
    /// </remarks>
    public class ForzaPacketTests
    {
        [Fact]
        public void PacketIsExactly324Bytes()
        {
            Assert.Equal(324, ForzaPacket.Size);
            Assert.Equal(324, ForzaPacket.CreateBuffer().Length);
        }

        [Fact]
        public void SpeedIsAtOffset256_TheAnchorForTheHorizonLayout()
        {
            var frame = new TelemetryFrame { Speed = 42.5f };
            var buf = ForzaPacket.CreateBuffer();

            ForzaPacket.Write(frame, buf);

            Assert.Equal(42.5f, BitConverter.ToSingle(buf, 256), 3);
        }

        [Fact]
        public void GearIsAtOffset319_TheSecondAnchor()
        {
            var frame = new TelemetryFrame { Gear = 4 };
            var buf = ForzaPacket.CreateBuffer();

            ForzaPacket.Write(frame, buf);

            Assert.Equal(4, buf[319]);
        }

        [Fact]
        public void EngineFieldsLandInTheSledHeader()
        {
            var frame = new TelemetryFrame
            {
                IsRaceOn         = true,
                TimestampMs      = 123456u,
                EngineMaxRpm     = 7500f,
                EngineIdleRpm    = 900f,
                CurrentEngineRpm = 4321f,
            };
            var buf = ForzaPacket.CreateBuffer();

            ForzaPacket.Write(frame, buf);

            Assert.Equal(1,       BitConverter.ToInt32(buf, 0));
            Assert.Equal(123456u, BitConverter.ToUInt32(buf, 4));
            Assert.Equal(7500f,   BitConverter.ToSingle(buf, 8),  3);
            Assert.Equal(900f,    BitConverter.ToSingle(buf, 12), 3);
            Assert.Equal(4321f,   BitConverter.ToSingle(buf, 16), 3);
        }

        [Fact]
        public void IsRaceOnIsZeroWhenNotDriving()
        {
            var buf = ForzaPacket.CreateBuffer();
            ForzaPacket.Write(new TelemetryFrame { IsRaceOn = false }, buf);
            Assert.Equal(0, BitConverter.ToInt32(buf, 0));
        }

        [Fact]
        public void WheelArraysAreOrderedFrontLeftFrontRightRearLeftRearRight()
        {
            var frame = new TelemetryFrame
            {
                TireSlipRatio = new WheelValues(1f, 2f, 3f, 4f)
            };
            var buf = ForzaPacket.CreateBuffer();

            ForzaPacket.Write(frame, buf);

            Assert.Equal(1f, BitConverter.ToSingle(buf, ForzaPacket.OffTireSlipRatio),      3);
            Assert.Equal(2f, BitConverter.ToSingle(buf, ForzaPacket.OffTireSlipRatio + 4),  3);
            Assert.Equal(3f, BitConverter.ToSingle(buf, ForzaPacket.OffTireSlipRatio + 8),  3);
            Assert.Equal(4f, BitConverter.ToSingle(buf, ForzaPacket.OffTireSlipRatio + 12), 3);
        }

        [Fact]
        public void RumbleStripIsEncodedAsInt32NotFloat()
        {
            // Forza declares this array as s32 while its neighbours are f32. Writing a
            // float here yields 1.0f == 0x3F800000, which a consumer reads as 1065353216.
            var frame = new TelemetryFrame { WheelOnRumbleStrip = WheelValues.Uniform(1f) };
            var buf = ForzaPacket.CreateBuffer();

            ForzaPacket.Write(frame, buf);

            Assert.Equal(1, BitConverter.ToInt32(buf, ForzaPacket.OffWheelOnRumbleStrip));
        }

        [Fact]
        public void HorizonPaddingBlockStaysZero()
        {
            var frame = new TelemetryFrame { Speed = 99f, PositionX = 5f };
            var buf = ForzaPacket.CreateBuffer();

            ForzaPacket.Write(frame, buf);

            for (int i = ForzaPacket.OffHorizonPadding; i < ForzaPacket.OffPositionX; i++)
                Assert.Equal(0, buf[i]);
        }

        [Fact]
        public void InputBytesOccupyTheirDistinctOffsets()
        {
            var frame = new TelemetryFrame
            {
                Accel     = 200,
                Brake     = 100,
                Clutch    = 50,
                HandBrake = 25,
                Gear      = 3,
                Steer     = -60,
            };
            var buf = ForzaPacket.CreateBuffer();

            ForzaPacket.Write(frame, buf);

            Assert.Equal(200, buf[315]);
            Assert.Equal(100, buf[316]);
            Assert.Equal(50,  buf[317]);
            Assert.Equal(25,  buf[318]);
            Assert.Equal(3,   buf[319]);
            Assert.Equal(-60, unchecked((sbyte)buf[320]));
        }

        [Fact]
        public void NegativeSteerSurvivesTheSignedByteRoundTrip()
        {
            var buf = ForzaPacket.CreateBuffer();
            ForzaPacket.Write(new TelemetryFrame { Steer = -127 }, buf);
            Assert.Equal(-127, unchecked((sbyte)buf[ForzaPacket.OffSteer]));
        }

        [Fact]
        public void SourceSentinelMarksPacketsAsOurs()
        {
            var buf = ForzaPacket.CreateBuffer();
            ForzaPacket.Write(new TelemetryFrame(), buf);
            Assert.Equal(ForzaPacket.SourceSentinel, buf[323]);
        }

        [Fact]
        public void BufferIsFullyOverwrittenSoItCanBeReusedEachFrame()
        {
            var buf = ForzaPacket.CreateBuffer();

            ForzaPacket.Write(new TelemetryFrame { Speed = 50f, Gear = 5 }, buf);
            ForzaPacket.Write(new TelemetryFrame { Speed = 0f },            buf);

            Assert.Equal(0f, BitConverter.ToSingle(buf, ForzaPacket.OffSpeed), 3);
            Assert.Equal(0,  buf[ForzaPacket.OffGear]);
        }

        [Fact]
        public void RejectsAnUndersizedBuffer()
        {
            var frame = new TelemetryFrame();
            Assert.Throws<ArgumentException>(() => ForzaPacket.Write(frame, new byte[323]));
        }

        [Fact]
        public void RejectsANullBuffer()
        {
            var frame = new TelemetryFrame();
            Assert.Throws<ArgumentNullException>(() => ForzaPacket.Write(frame, null));
        }

        [Theory]
        [InlineData(-0.5f, 0)]
        [InlineData(0f,    0)]
        [InlineData(0.5f,  128)]
        [InlineData(1f,    255)]
        [InlineData(1.5f,  255)]
        public void PedalScalingClampsOutOfRangeInput(float input, byte expected)
        {
            // Smoothed inputs overshoot; an unchecked cast of 1.01f would wrap to ~2.
            Assert.Equal(expected, TelemetryFrame.ToPedal(input));
        }

        [Theory]
        [InlineData(-2f,   -127)]
        [InlineData(-1f,   -127)]
        [InlineData(0f,    0)]
        [InlineData(1f,    127)]
        [InlineData(2f,    127)]
        public void SteerScalingClampsOutOfRangeInput(float input, sbyte expected)
        {
            Assert.Equal(expected, TelemetryFrame.ToSteer(input));
        }
    }
}
