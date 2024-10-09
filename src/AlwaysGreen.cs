using System;
using System.Collections.Generic;
using GTA;
using GTAN = GTA.Native;

namespace AlwaysGreen
{
    public class AlwaysGreen : Script
    {
        private int[] _hashes = { 1043035044, -655644382, 865627822, 656557234, 862871082 };
        private float _spotOffset = 0;
        private List<Vehicle> _traffic = new List<Vehicle>();
        private System.Diagnostics.Stopwatch _tick = new System.Diagnostics.Stopwatch();
        

        public AlwaysGreen()
        {
            KeyDown += (object sender, System.Windows.Forms.KeyEventArgs e) =>
            {
                // TODO: Probably it should has a rotation limit. 30-50 deg?
                _spotOffset -= Convert.ToByte(GTA.Game.IsControlPressed(0, Control.MoveLeftOnly));
                _spotOffset += Convert.ToByte(GTA.Game.IsControlPressed(0, Control.MoveRightOnly));
            };


            KeyUp += (object sender, System.Windows.Forms.KeyEventArgs e) =>
            {
                _spotOffset *= Convert.ToByte(GTA.Game.IsControlPressed(0, Control.MoveLeftOnly)) | Convert.ToByte(GTA.Game.IsControlPressed(0, Control.MoveRightOnly));
            };


            Tick += (object sender, EventArgs e) =>
            {
                // If you have a good CPU, you can lower the timing or even turn it off.
                if (_tick.ElapsedMilliseconds < 500 || GTA.Game.Player.Character.IsInVehicle() == false) return;

                // 100 is a max distance when the game can detect object as a traffic light.
                float distance = Math.Min(30 + GTA.Game.Player.Character.CurrentVehicle.Speed, 100);

                #if DEBUG // Debug drawing.
                {
                    var pos = GTA.Game.Player.Character.Position;
                    var spot = GTA.Game.Player.Character.GetOffsetInWorldCoords(new GTA.Math.Vector3(13 + _spotOffset, distance, 0));
                    GTAN.Function.Call(GTAN.Hash.DRAW_LINE, pos.X, pos.Y, pos.Z, spot.X, spot.Y, spot.Z, 255, 255, 255, 255);
                    GTAN.Function.Call(GTAN.Hash.DRAW_BOX, spot.X - 7, spot.Y - 7, spot.Z - 7, spot.X + 7, spot.Y + 7, spot.Z + 7, 255, 255, 255, 50);

                    spot = GTA.Game.Player.Character.GetOffsetInWorldCoords(new GTA.Math.Vector3(-5 + _spotOffset, distance, 0));
                    GTAN.Function.Call(GTAN.Hash.DRAW_LINE, pos.X, pos.Y, pos.Z, spot.X, spot.Y, spot.Z, 255, 255, 255, 255);
                    GTAN.Function.Call(GTAN.Hash.DRAW_BOX, spot.X - 7, spot.Y - 7, spot.Z - 7, spot.X + 7, spot.Y + 7, spot.Z + 7, 255, 255, 255, 50);
                }
                #endif


                List<Entity> enteties = new List<Entity>();
                distance -= _spotOffset;

                // The radius more than 30 can be very hard for CPU.
                enteties.AddRange(GTA.World.GetNearbyEntities(GTA.Game.Player.Character.GetOffsetInWorldCoords(new GTA.Math.Vector3(13  + _spotOffset, distance, 0)), 25f));
                enteties.AddRange(GTA.World.GetNearbyEntities(GTA.Game.Player.Character.GetOffsetInWorldCoords(new GTA.Math.Vector3(-55 + _spotOffset, distance, 0)), 25f));


                int changingTL = 0, crossingTL = 0, playerStreet = 0, playerXStreet = 0;
                var playerPos = GTA.Game.Player.Character.Position;
                foreach (var item in enteties)
                {
                    // Detect objects on player's road.
                    unsafe
                    {
                        GTAN.Function.Call(GTAN.Hash.GET_STREET_NAME_AT_COORD, item.Position.X, item.Position.Y, item.Position.Z, &changingTL, &crossingTL);
                        GTAN.Function.Call(GTAN.Hash.GET_STREET_NAME_AT_COORD, playerPos.X, playerPos.Y, playerPos.Z, &playerStreet, &playerXStreet);
                    }
                    
                    if (changingTL == playerStreet && GTAN.Function.Call<bool>(GTAN.Hash.IS_ENTITY_A_VEHICLE, item) && ((GTA.Vehicle)item).Driver != GTA.Game.Player.Character && ((GTA.Vehicle)item).Speed < 5)
                    {
                        GTAN.Function.Call<bool>(GTAN.Hash.SET_DRIVER_ABILITY, ((GTA.Vehicle)item).Driver, 100.0f);
                        GTAN.Function.Call<bool>(GTAN.Hash.TASK_VEHICLE_DRIVE_WANDER, ((GTA.Vehicle)item).Driver, (GTA.Vehicle)item, 70, (int)GTA.DrivingStyle.IgnoreLights);
                        _traffic.Add(((GTA.Vehicle)item));
                    }


                    foreach (var hash in _hashes)
                    {
                        if (hash == item.Model.Hash)
                        {
                            // TODO: Improve it and make switching of the crossing traffic lights to red and stop cars from other directions.

                            GTAN.Function.Call(GTAN.Hash.SET_ENTITY_TRAFFICLIGHT_OVERRIDE, item,
                                GTAN.Function.Call<float>(GTAN.Hash.GET_ANGLE_BETWEEN_2D_VECTORS, item.ForwardVector.X,
                                item.ForwardVector.Y, GTA.Game.Player.Character.ForwardVector.X, GTA.Game.Player.Character.ForwardVector.Y) >= 90);
                        }
                    }                    
                }

                for (int i = _traffic.Count - 1; i >= 0; --i)
                {
                    if (_traffic[i].Driver.Position.DistanceTo2D(Game.Player.Character.Position) > 100)
                    {
                        GTAN.Function.Call(GTAN.Hash.TASK_VEHICLE_DRIVE_WANDER, _traffic[i].Driver, _traffic[i], 70, (int)GTA.DrivingStyle.Normal);
                        _traffic.RemoveAt(i);
                    }
                }

                _tick.Restart();
            };

            _tick.Restart();
        }
    }
}
