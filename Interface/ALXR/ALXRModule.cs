using BaseX;
using FrooxEngine;
using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace QuestProModule.ALXR
{
    public class ALXRModule : IQuestProModule
    {
        private IPAddress localAddr;
        private const int DEFAULT_PORT = 13191;
        
        private TcpClient client;
        private NetworkStream stream;
        private Thread tcpThread;
        private CancellationTokenSource cancellationTokenSource;
        private bool connected = false;

        private const int NATURAL_EXPRESSIONS_COUNT = 63;
        private const float SRANIPAL_NORMALIZER = 0.75f;
        private byte[] rawExpressions = new byte[NATURAL_EXPRESSIONS_COUNT * 4 + (8 * 2 * 4)];
        private float[] expressions = new float[NATURAL_EXPRESSIONS_COUNT + (8 * 2)];

        private double pitch_L, yaw_L, pitch_R, yaw_R; // Eye rotations

        public bool Initialize()
        {
            localAddr = QuestProMod.config.GetValue(QuestProMod.QuestProIP);
            cancellationTokenSource = new CancellationTokenSource();
            ConnectToTCP(); // Will this block the main thread?

            tcpThread = new Thread(Update);
            tcpThread.Start();

            return true;
        }

        private bool ConnectToTCP()
        {
            try
            {
                localAddr = QuestProMod.config.GetValue(QuestProMod.QuestProIP);

                client = new TcpClient();
                UniLog.Log($"Trying to establish a Quest Pro connection at {localAddr}:{DEFAULT_PORT}...");

                client.Connect(localAddr, DEFAULT_PORT);
                UniLog.Log("Connected to Quest Pro!");

                stream = client.GetStream();
                connected = true;

                return true;
            }
            catch (Exception e)
            {
                UniLog.Error(e.Message);
                return false;
            }
        }

        public void Update()
        {
            while (!cancellationTokenSource.IsCancellationRequested)
            {
                try
                {
                    // Attempt reconnection if needed
                    if (!connected || stream == null)
                    {
                        ConnectToTCP();
                    }

                    // If the connection was unsuccessful, wait a bit and try again
                    if (stream == null)
                    {
                        UniLog.Warning("Didn't reconnect to the Quest Pro just yet! Trying again...");
                        return;
                    }

                    if (!stream.CanRead)
                    {
                        UniLog.Warning("Can't read from network stream just yet! Trying again...");
                        return;
                    }

                    int offset = 0;
                    int readBytes;
                    do
                    {
                        readBytes = stream.Read(rawExpressions, offset, rawExpressions.Length - offset);
                        offset += readBytes;
                    }
                    while (readBytes > 0 && offset < rawExpressions.Length);

                    if (offset < rawExpressions.Length && connected)
                    {
                        UniLog.Warning("End of stream! Reconnecting...");
                        Thread.Sleep(1000);
                        connected = false;
                        try
                        {
                            stream.Close();
                        }
                        catch (SocketException e)
                        {
                            UniLog.Error(e.Message);
                            Thread.Sleep(1000);
                        }
                    }

                    // We receive information from the stream as a byte array 63*4 bytes long, since floats are 32 bits long and we have 63 expressions.
                    // We then need to convert these bytes to a floats. I've opted to use Buffer.BlockCopy instead of BitConverter.ToSingle, since it's faster.
                    Buffer.BlockCopy(rawExpressions, 0, expressions, 0, NATURAL_EXPRESSIONS_COUNT * 4 + (8 * 2 * 4));

                    // Preprocess our expressions per Meta's Documentation
                    PrepareUpdate();
                }
                catch (SocketException e)
                {
                    UniLog.Error(e.Message);
                    Thread.Sleep(1000);
                }
                
            }         
        }
    
        private void PrepareUpdate()
        {
            // Eye Expressions

            double q_x = expressions[64];
            double q_y = expressions[65];
            double q_z = expressions[66];
            double q_w = expressions[67];

            double yaw = Math.Atan2(2.0 * (q_y * q_z + q_w * q_x), q_w * q_w - q_x * q_x - q_y * q_y + q_z * q_z);
            double pitch = Math.Asin(-2.0 * (q_x * q_z - q_w * q_y));
            // Not needed for eye tracking
            // double roll = Math.Atan2(2.0 * (q_x * q_y + q_w * q_z), q_w * q_w + q_x * q_x - q_y * q_y - q_z * q_z); 

            // From radians
            pitch_L = 180.0 / Math.PI * pitch; 
            yaw_L = 180.0 / Math.PI * yaw;

            q_x = expressions[72];
            q_y = expressions[73];
            q_z = expressions[74];
            q_w = expressions[75];

            yaw = Math.Atan2(2.0 * (q_y * q_z + q_w * q_x), q_w * q_w - q_x * q_x - q_y * q_y + q_z * q_z);
            pitch = Math.Asin(-2.0 * (q_x * q_z - q_w * q_y));

            // From radians
            pitch_R = 180.0 / Math.PI * pitch; 
            yaw_R = 180.0 / Math.PI * yaw;

            // Face Expressions

            // Eyelid edge case, eyes are actually closed now
            if (expressions[FBExpression.Eyes_Look_Down_L] == expressions[FBExpression.Eyes_Look_Up_L] && expressions[FBExpression.Eyes_Closed_L] > 0.25f)
            { 
                expressions[FBExpression.Eyes_Closed_L] = 0; // 0.9f - (expressions[FBExpression.Lid_Tightener_L] * 3);
            }
            else
            {
                expressions[FBExpression.Eyes_Closed_L] = 0.9f - ((expressions[FBExpression.Eyes_Closed_L] * 3) / (1 + expressions[FBExpression.Eyes_Look_Down_L] * 3));
            }

            // Another eyelid edge case
            if (expressions[FBExpression.Eyes_Look_Down_R] == expressions[FBExpression.Eyes_Look_Up_R] && expressions[FBExpression.Eyes_Closed_R] > 0.25f)
            { 
                expressions[FBExpression.Eyes_Closed_R] = 0; // 0.9f - (expressions[FBExpression.Lid_Tightener_R] * 3);
            }
            else
            {
                expressions[FBExpression.Eyes_Closed_R] = 0.9f - ((expressions[FBExpression.Eyes_Closed_R] * 3) / (1 + expressions[FBExpression.Eyes_Look_Down_R] * 3));
            }

            //expressions[FBExpression.Lid_Tightener_L = 0.8f-expressions[FBExpression.Eyes_Closed_L]; // Sad: fix combined param instead
            //expressions[FBExpression.Lid_Tightener_R = 0.8f-expressions[FBExpression.Eyes_Closed_R]; // Sad: fix combined param instead

            if (1 - expressions[FBExpression.Eyes_Closed_L] < expressions[FBExpression.Lid_Tightener_L])
                expressions[FBExpression.Lid_Tightener_L] = (1 - expressions[FBExpression.Eyes_Closed_L]) - 0.01f;

            if (1 - expressions[FBExpression.Eyes_Closed_R] < expressions[FBExpression.Lid_Tightener_R])
                expressions[FBExpression.Lid_Tightener_R] = (1 - expressions[FBExpression.Eyes_Closed_R]) - 0.01f;

            //expressions[FBExpression.Lid_Tightener_L = Math.Max(0, expressions[FBExpression.Lid_Tightener_L] - 0.15f);
            //expressions[FBExpression.Lid_Tightener_R = Math.Max(0, expressions[FBExpression.Lid_Tightener_R] - 0.15f);

            expressions[FBExpression.Upper_Lid_Raiser_L] = Math.Max(0, expressions[FBExpression.Upper_Lid_Raiser_L] - 0.5f);
            expressions[FBExpression.Upper_Lid_Raiser_R] = Math.Max(0, expressions[FBExpression.Upper_Lid_Raiser_R] - 0.5f);

            expressions[FBExpression.Lid_Tightener_L] = Math.Max(0, expressions[FBExpression.Lid_Tightener_L] - 0.5f);
            expressions[FBExpression.Lid_Tightener_R] = Math.Max(0, expressions[FBExpression.Lid_Tightener_R] - 0.5f);

            expressions[FBExpression.Inner_Brow_Raiser_L] = Math.Min(1, expressions[FBExpression.Inner_Brow_Raiser_L] * 3f); // * 4;
            expressions[FBExpression.Brow_Lowerer_L] = Math.Min(1, expressions[FBExpression.Brow_Lowerer_L] * 3f); // * 4;
            expressions[FBExpression.Outer_Brow_Raiser_L] = Math.Min(1, expressions[FBExpression.Outer_Brow_Raiser_L] * 3f); // * 4;

            expressions[FBExpression.Inner_Brow_Raiser_R] = Math.Min(1, expressions[FBExpression.Inner_Brow_Raiser_R] * 3f); // * 4;
            expressions[FBExpression.Brow_Lowerer_R] = Math.Min(1, expressions[FBExpression.Brow_Lowerer_R] * 3f); // * 4;
            expressions[FBExpression.Outer_Brow_Raiser_R] = Math.Min(1, expressions[FBExpression.Outer_Brow_Raiser_R] * 3f); // * 4;

            expressions[FBExpression.Eyes_Look_Up_L] = expressions[FBExpression.Eyes_Look_Up_L] * 0.55f;
            expressions[FBExpression.Eyes_Look_Up_R] = expressions[FBExpression.Eyes_Look_Up_R] * 0.55f;
            expressions[FBExpression.Eyes_Look_Down_L] = expressions[FBExpression.Eyes_Look_Down_L] * 1.5f;
            expressions[FBExpression.Eyes_Look_Down_R] = expressions[FBExpression.Eyes_Look_Down_R] * 1.5f;

            expressions[FBExpression.Eyes_Look_Left_L] = expressions[FBExpression.Eyes_Look_Left_L] * 0.85f;
            expressions[FBExpression.Eyes_Look_Right_L] = expressions[FBExpression.Eyes_Look_Right_L] * 0.85f;
            expressions[FBExpression.Eyes_Look_Left_R] = expressions[FBExpression.Eyes_Look_Left_R] * 0.85f;
            expressions[FBExpression.Eyes_Look_Right_R] = expressions[FBExpression.Eyes_Look_Right_R] * 0.85f;

            // Hack: turn rots to looks
            // Yitch = 29(left)-- > -29(right)
            // Yaw = -27(down)-- > 27(up)

            if (pitch_L > 0)
            {
                expressions[FBExpression.Eyes_Look_Left_L] = Math.Min(1, (float)(pitch_L / 29.0)) * SRANIPAL_NORMALIZER;
                expressions[FBExpression.Eyes_Look_Right_L] = 0;
            }
            else
            {
                expressions[FBExpression.Eyes_Look_Left_L] = 0;
                expressions[FBExpression.Eyes_Look_Right_L] = Math.Min(1, (float)((-pitch_L) / 29.0)) * SRANIPAL_NORMALIZER;
            }

            if (yaw_L > 0)
            {
                expressions[FBExpression.Eyes_Look_Up_L] = Math.Min(1, (float)(yaw_L / 27.0)) * SRANIPAL_NORMALIZER;
                expressions[FBExpression.Eyes_Look_Down_L] = 0;
            }
            else
            {
                expressions[FBExpression.Eyes_Look_Up_L] = 0;
                expressions[FBExpression.Eyes_Look_Down_L] = Math.Min(1, (float)((-yaw_L) / 27.0)) * SRANIPAL_NORMALIZER;
            }


            if (pitch_R > 0)
            {
                expressions[FBExpression.Eyes_Look_Left_R] = Math.Min(1, (float)(pitch_R / 29.0)) * SRANIPAL_NORMALIZER;
                expressions[FBExpression.Eyes_Look_Right_R] = 0;
            }
            else
            {
                expressions[FBExpression.Eyes_Look_Left_R] = 0;
                expressions[FBExpression.Eyes_Look_Right_R] = Math.Min(1, (float)((-pitch_R) / 29.0)) * SRANIPAL_NORMALIZER;
            }
            
            if (yaw_R > 0)
            {
                expressions[FBExpression.Eyes_Look_Up_R] = Math.Min(1, (float)(yaw_R / 27.0)) * SRANIPAL_NORMALIZER;
                expressions[FBExpression.Eyes_Look_Down_R] = 0;
            }
            else
            {
                expressions[FBExpression.Eyes_Look_Up_R] = 0;
                expressions[FBExpression.Eyes_Look_Down_R] = Math.Min(1, (float)((-yaw_R) / 27.0)) * SRANIPAL_NORMALIZER;
            }
        }

        public void Teardown()
        {
            cancellationTokenSource.Cancel();
            tcpThread.Abort();
            cancellationTokenSource.Dispose();
            stream.Close();
            stream.Dispose();
            client.Close();
            client.Dispose();
        }

        public void GetEyeExpressions(FBEye fbEye, in FrooxEngine.Eye frooxEye)
        {
            frooxEye.IsDeviceActive = Engine.Current.InputInterface.VR_Active;
            frooxEye.IsTracking = Engine.Current.InputInterface.VR_Active;
            frooxEye.PupilDiameter = 0.0035f;
            frooxEye.Frown = 0f;
            
            switch (fbEye)
            {
                case FBEye.Left:
                    frooxEye.RawPosition = new float3(expressions[68], expressions[69], expressions[70]);
                    frooxEye.RawRotation = new floatQ(expressions[64], expressions[65], expressions[66], expressions[67]);
                    frooxEye.Openness = expressions[FBExpression.Eyes_Closed_L];
                    frooxEye.Squeeze = expressions[FBExpression.Lid_Tightener_L];
                    frooxEye.Widen = expressions[FBExpression.Upper_Lid_Raiser_L];
                    break;
                case FBEye.Right:
                    frooxEye.RawPosition = new float3(expressions[76], expressions[77], expressions[78]);
                    frooxEye.RawRotation = new floatQ(expressions[72], expressions[73], expressions[74], expressions[75]);
                    frooxEye.Openness = expressions[FBExpression.Eyes_Closed_R];
                    frooxEye.Squeeze = expressions[FBExpression.Lid_Tightener_R];
                    frooxEye.Widen = expressions[FBExpression.Upper_Lid_Raiser_R];
                    break;
                case FBEye.Combined:
                    frooxEye.RawPosition = MathX.Average(new float3(expressions[68], expressions[69], expressions[70]), new float3(expressions[76], expressions[77], expressions[78]));
                    frooxEye.RawRotation = MathX.Slerp(new floatQ(expressions[64], expressions[65], expressions[66], expressions[67]), new floatQ(expressions[72], expressions[73], expressions[74], expressions[75]), 0.5f); // Compute the midpoint by slerping from one quaternion to the other
                    frooxEye.Openness = (expressions[FBExpression.Eyes_Closed_R] + expressions[FBExpression.Eyes_Closed_R]) / 2.0f;
                    frooxEye.Squeeze = (expressions[FBExpression.Lid_Tightener_R] + expressions[FBExpression.Lid_Tightener_R]) / 2.0f;
                    frooxEye.Widen = (expressions[FBExpression.Upper_Lid_Raiser_R] + expressions[FBExpression.Upper_Lid_Raiser_R]) / 2.0f;
                    break;
            }
        }

        // TODO: Double check jaw movements and mappings
        public void GetFacialExpressions(in FrooxEngine.Mouth mouth)
        {
            mouth.IsDeviceActive = Engine.Current.InputInterface.VR_Active;
            mouth.IsTracking = Engine.Current.InputInterface.VR_Active;

            mouth.JawOpen = expressions[FBExpression.Jaw_Drop];
            
            mouth.Jaw = new float3( 
                expressions[FBExpression.Jaw_Sideways_Left] - expressions[FBExpression.Jaw_Sideways_Right],
                expressions[FBExpression.Jaw_Thrust],
                0f
            );

            mouth.LipUpperLeftRaise = expressions[FBExpression.Mouth_Left];
            mouth.LipUpperRightRaise = expressions[FBExpression.Mouth_Right];
            mouth.LipLowerLeftRaise = expressions[FBExpression.Mouth_Left];
            mouth.LipLowerRightRaise = expressions[FBExpression.Mouth_Right];

            var stretch = (expressions[FBExpression.Lip_Stretcher_L] + expressions[FBExpression.Lip_Stretcher_R]) / 2;
            mouth.LipUpperHorizontal = stretch;
            mouth.LipLowerHorizontal = stretch;

            mouth.MouthLeftSmileFrown = Math.Min(1, expressions[FBExpression.Lip_Corner_Puller_L] * 1.2f) - Math.Min(1, (expressions[FBExpression.Lip_Corner_Depressor_L] + expressions[FBExpression.Lip_Stretcher_L]) * 0.75f);//Math.Min(1, (expressions[FBExpression.Lip_Corner_Depressor_L]) * 1.5f);;
            mouth.MouthRightSmileFrown = Math.Min(1, expressions[FBExpression.Lip_Corner_Puller_R] * 1.2f) - Math.Min(1, (expressions[FBExpression.Lip_Corner_Depressor_R] + expressions[FBExpression.Lip_Stretcher_R]) * 0.75f);//Math.Min(1, (expressions[FBExpression.Lip_Corner_Depressor_R]) * 1.5f);;
            
            mouth.MouthPout = (expressions[FBExpression.Lip_Pucker_L] + expressions[FBExpression.Lip_Pucker_R]) / 3;

            // mouth.LipTopOverUnder = (expressions[FBExpression.Lip_Suck_LT] + expressions[FBExpression.Lip_Suck_RT]) / 2;
            // mouth.LipBottomOverturn = (expressions[FBExpression.Lip_Suck_LB] + expressions[FBExpression.Lip_Suck_RB]) / 2;

            mouth.LipTopOverturn = (expressions[FBExpression.Lips_Toward] + expressions[FBExpression.Lip_Funneler_LT] + expressions[FBExpression.Lip_Funneler_RT]) / 3;
            mouth.LipBottomOverturn = (expressions[FBExpression.Lips_Toward] + expressions[FBExpression.Lip_Funneler_LB] + expressions[FBExpression.Lip_Funneler_RB]) / 3;

            //if (UnifiedTrackingData.LatestLipData.LatestShapes[(int)UnifiedExpression.MouthSmileLeft] > UnifiedTrackingData.LatestLipData.LatestShapes[(int)UnifiedExpression.MouthSadLeft])
            //    UnifiedTrackingData.LatestLipData.LatestShapes[(int)UnifiedExpression.MouthSadLeft] /= 1 + UnifiedTrackingData.LatestLipData.LatestShapes[(int)UnifiedExpression.MouthSmileLeft];
            //else if (UnifiedTrackingData.LatestLipData.LatestShapes[(int)UnifiedExpression.MouthSmileLeft] < UnifiedTrackingData.LatestLipData.LatestShapes[(int)UnifiedExpression.MouthSadLeft])
            //    UnifiedTrackingData.LatestLipData.LatestShapes[(int)UnifiedExpression.MouthSmileLeft] /= 1 + UnifiedTrackingData.LatestLipData.LatestShapes[(int)UnifiedExpression.MouthSadLeft];

            //if (UnifiedTrackingData.LatestLipData.LatestShapes[(int)UnifiedExpression.MouthSmileRight] > UnifiedTrackingData.LatestLipData.LatestShapes[(int)UnifiedExpression.MouthSadRight])
            //    UnifiedTrackingData.LatestLipData.LatestShapes[(int)UnifiedExpression.MouthSadRight] /= 1 + UnifiedTrackingData.LatestLipData.LatestShapes[(int)UnifiedExpression.MouthSmileRight];
            //else if (UnifiedTrackingData.LatestLipData.LatestShapes[(int)UnifiedExpression.MouthSmileRight] < UnifiedTrackingData.LatestLipData.LatestShapes[(int)UnifiedExpression.MouthSadRight])
            //    UnifiedTrackingData.LatestLipData.LatestShapes[(int)UnifiedExpression.MouthSmileRight] /= 1 + UnifiedTrackingData.LatestLipData.LatestShapes[(int)UnifiedExpression.MouthSadRight];

            var cheekSuck = (expressions[FBExpression.Cheek_Suck_L] + expressions[FBExpression.Cheek_Suck_R]) / 2;
            mouth.CheekLeftPuffSuck = expressions[FBExpression.Cheek_Puff_L] - cheekSuck;
            mouth.CheekRightPuffSuck = expressions[FBExpression.Cheek_Puff_R] - cheekSuck;
        }

        public float GetFaceExpression(int expressionIndex)
        {
            return expressions[expressionIndex];
        }

        public enum FBEye
        {
            Left,
            Right,
            Combined
        }
    }
}
