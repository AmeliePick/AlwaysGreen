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
        //private System.Diagnostics.Stopwatch _tick = new System.Diagnostics.Stopwatch();
        

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
                // IF your CPU is bottlenecked you should uncomment this line and all _tick.Restart();
                // After this the script will request game data by specified interval and not on each frame.
                if (/*_tick.ElapsedMilliseconds < 500 || */GTA.Game.Player.Character.IsInVehicle() == false) return;


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
                List<Entity> tls = new List<Entity>();
                distance -= _spotOffset;

                // The radius more than 30 can be very hard for CPU.
                GTA.Math.Vector3 leftSpot  = GTA.Game.Player.Character.GetOffsetInWorldCoords(new GTA.Math.Vector3(13 + _spotOffset, distance, 0));
                GTA.Math.Vector3 rightSpot = GTA.Game.Player.Character.GetOffsetInWorldCoords(new GTA.Math.Vector3(-55 + _spotOffset, distance, 0));

                foreach (var hash in _hashes)
                {
                    var obj = GTAN.Function.Call<GTA.Entity>(GTAN.Hash.GET_CLOSEST_OBJECT_OF_TYPE, leftSpot.X, leftSpot.Y, leftSpot.Z, 25f, hash, false, false, false);
                    if (obj != null) tls.Add(obj);

                    obj = GTAN.Function.Call<GTA.Entity>(GTAN.Hash.GET_CLOSEST_OBJECT_OF_TYPE, rightSpot.X, rightSpot.Y, rightSpot.Z, 25f, hash, false, false, false);
                    if (obj != null) tls.Add(obj);
                }
                
                if (tls.Count == 0) return;
                tls.Clear();
                enteties.AddRange(GTA.World.GetNearbyEntities(leftSpot, 25f));
                enteties.AddRange(GTA.World.GetNearbyEntities(rightSpot, 25f));

                foreach (var item in enteties)
                {
                    int type = GTAN.Function.Call<int>(GTAN.Hash.GET_ENTITY_TYPE, item);
                    if (type == 0) continue;

                    // Detect objects on player's road.
                    bool entityOnTheWay = GTAN.Function.Call<float>(GTAN.Hash.GET_ANGLE_BETWEEN_2D_VECTORS, item.Position.X, item.Position.Y,
                                          GTA.Game.Player.Character.Position.X, GTA.Game.Player.Character.Position.Y) <= 45;


                    if (type == 1 && (GTA.Ped)item != Game.Player.Character) ((GTA.Ped)item).Task.StandStill(5000);
                    else if (type == 2 && ((GTA.Vehicle)item).Driver != Game.Player.Character)
                    {
                        if (!entityOnTheWay) ((GTA.Vehicle)item).Driver.Task.StandStill(5000);
                        else
                        {
                            GTAN.Function.Call<bool>(GTAN.Hash.SET_DRIVER_ABILITY, ((GTA.Vehicle)item).Driver, 100.0f);
                            GTAN.Function.Call<bool>(GTAN.Hash.TASK_VEHICLE_DRIVE_WANDER, ((GTA.Vehicle)item).Driver, (GTA.Vehicle)item, 70, (int)GTA.DrivingStyle.IgnoreLights);
                            _traffic.Add(((GTA.Vehicle)item));
                        }
                    }
                    else if (type == 3)
                    {
                        foreach (var hash in _hashes)
                        {
                            if (hash == item.Model.Hash)
                            {
                                // TODO: Improve it and make switching of the crossing traffic lights to red and stop cars from other directions.

                                GTAN.Function.Call(GTAN.Hash.SET_ENTITY_TRAFFICLIGHT_OVERRIDE, item, !entityOnTheWay);
                                break;
                            }
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

                //_tick.Restart();
            };

            //_tick.Restart();
        }
    }
}
