using BaseX;
using OscCore;
using System;

namespace QuestProModule;

public class FbMessage
{
  private const int NaturalExpressionsCount = 63;
  private const float SranipalNormalizer = 0.75f;
  public readonly float[] Expressions = new float[NaturalExpressionsCount + 8 * 2];

  public void ParseOsc(OscMessageRaw message)
  {	
	int index = 0;
	if (message.Address == "/tracking/eye/left/Quat")
	{
	  Array.Clear(Expressions, FbExpression.LeftRot_w, 4);
	  index = FbExpression.LeftRot_w;
	} else if (message.Address == "/tracking/eye/right/Quat")
	{
	  Array.Clear(Expressions, FbExpression.RightRot_w, 4);
	  index = FbExpression.RightRot_w;
	} else
	{
	  Array.Clear(Expressions, 0, 63);
	}
    foreach (var arg in message)
    {
      // this osc library is strange.
      var localArg = arg;
      Expressions[index] = message.ReadFloat(ref localArg);

      index++;
    }

    //// Clear the rest if it wasn't long enough for some reason.
    //for (; index < Expressions.Length; index++)
    //{
    //  Expressions[index] = 0.0f;
    //}

	// Im not sure why this was done, but what I am sure of is that this breaks the eye look by setting it to 0

    PrepareUpdate();
  }

  private static bool FloatNear(float f1, float f2) => Math.Abs(f1 - f2) < 0.0001;

  private void PrepareUpdate()
  {
    // Eye Expressions

    double qX = Expressions[FbExpression.LeftRot_x];
    double qY = Expressions[FbExpression.LeftRot_y];
    double qZ = Expressions[FbExpression.LeftRot_z];
    double qW = Expressions[FbExpression.LeftRot_w];

    double yaw = Math.Atan2(2.0 * (qY * qZ + qW * qX), qW * qW - qX * qX - qY * qY + qZ * qZ);
    double pitch = Math.Asin(-2.0 * (qX * qZ - qW * qY));
    // Not needed for eye tracking
    // double roll = Math.Atan2(2.0 * (q_x * q_y + q_w * q_z), q_w * q_w + q_x * q_x - q_y * q_y - q_z * q_z); 

    // From radians
    double pitchL = 180.0 / Math.PI * pitch;
    double yawL = 180.0 / Math.PI * yaw;

    qX = Expressions[FbExpression.RightRot_x];
    qY = Expressions[FbExpression.RightRot_y];
    qZ = Expressions[FbExpression.RightRot_z];
    qW = Expressions[FbExpression.RightRot_w];

    yaw = Math.Atan2(2.0 * (qY * qZ + qW * qX), qW * qW - qX * qX - qY * qY + qZ * qZ);
    pitch = Math.Asin(-2.0 * (qX * qZ - qW * qY));

    // From radians
    double pitchR = 180.0 / Math.PI * pitch;
    double yawR = 180.0 / Math.PI * yaw;

    // Face Expressions

    // Eyelid edge case, eyes are actually closed now
    if (FloatNear(Expressions[FbExpression.Eyes_Look_Down_L], Expressions[FbExpression.Eyes_Look_Up_L]) &&
        Expressions[FbExpression.Eyes_Closed_L] > 0.25f)
    {
      Expressions[FbExpression.Eyes_Closed_L] = 0; // 0.9f - (expressions[FBExpression.Lid_Tightener_L] * 3);
    }
    else
    {
      Expressions[FbExpression.Eyes_Closed_L] = 0.9f - Expressions[FbExpression.Eyes_Closed_L] * 3 /
        (1 + Expressions[FbExpression.Eyes_Look_Down_L] * 3);
    }

    // Another eyelid edge case
    if (FloatNear(Expressions[FbExpression.Eyes_Look_Down_R], Expressions[FbExpression.Eyes_Look_Up_R]) &&
        Expressions[FbExpression.Eyes_Closed_R] > 0.25f)
    {
      Expressions[FbExpression.Eyes_Closed_R] = 0; // 0.9f - (expressions[FBExpression.Lid_Tightener_R] * 3);
    }
    else
    {
      Expressions[FbExpression.Eyes_Closed_R] = 0.9f - Expressions[FbExpression.Eyes_Closed_R] * 3 /
        (1 + Expressions[FbExpression.Eyes_Look_Down_R] * 3);
    }

    //expressions[FBExpression.Lid_Tightener_L = 0.8f-expressions[FBExpression.Eyes_Closed_L]; // Sad: fix combined param instead
    //expressions[FBExpression.Lid_Tightener_R = 0.8f-expressions[FBExpression.Eyes_Closed_R]; // Sad: fix combined param instead

    if (1 - Expressions[FbExpression.Eyes_Closed_L] < Expressions[FbExpression.Lid_Tightener_L])
      Expressions[FbExpression.Lid_Tightener_L] = 1 - Expressions[FbExpression.Eyes_Closed_L] - 0.01f;

    if (1 - Expressions[FbExpression.Eyes_Closed_R] < Expressions[FbExpression.Lid_Tightener_R])
      Expressions[FbExpression.Lid_Tightener_R] = 1 - Expressions[FbExpression.Eyes_Closed_R] - 0.01f;

    //expressions[FBExpression.Lid_Tightener_L = Math.Max(0, expressions[FBExpression.Lid_Tightener_L] - 0.15f);
    //expressions[FBExpression.Lid_Tightener_R = Math.Max(0, expressions[FBExpression.Lid_Tightener_R] - 0.15f);

    Expressions[FbExpression.Upper_Lid_Raiser_L] = Math.Max(0, Expressions[FbExpression.Upper_Lid_Raiser_L] - 0.5f);
    Expressions[FbExpression.Upper_Lid_Raiser_R] = Math.Max(0, Expressions[FbExpression.Upper_Lid_Raiser_R] - 0.5f);

    Expressions[FbExpression.Lid_Tightener_L] = Math.Max(0, Expressions[FbExpression.Lid_Tightener_L] - 0.5f);
    Expressions[FbExpression.Lid_Tightener_R] = Math.Max(0, Expressions[FbExpression.Lid_Tightener_R] - 0.5f);

    Expressions[FbExpression.Inner_Brow_Raiser_L] =
      Math.Min(1, Expressions[FbExpression.Inner_Brow_Raiser_L] * 3f); // * 4;
    Expressions[FbExpression.Brow_Lowerer_L] = Math.Min(1, Expressions[FbExpression.Brow_Lowerer_L] * 3f); // * 4;
    Expressions[FbExpression.Outer_Brow_Raiser_L] =
      Math.Min(1, Expressions[FbExpression.Outer_Brow_Raiser_L] * 3f); // * 4;

    Expressions[FbExpression.Inner_Brow_Raiser_R] =
      Math.Min(1, Expressions[FbExpression.Inner_Brow_Raiser_R] * 3f); // * 4;
    Expressions[FbExpression.Brow_Lowerer_R] = Math.Min(1, Expressions[FbExpression.Brow_Lowerer_R] * 3f); // * 4;
    Expressions[FbExpression.Outer_Brow_Raiser_R] =
      Math.Min(1, Expressions[FbExpression.Outer_Brow_Raiser_R] * 3f); // * 4;

    Expressions[FbExpression.Eyes_Look_Up_L] *= 0.55f;
    Expressions[FbExpression.Eyes_Look_Up_R] *= 0.55f;
    Expressions[FbExpression.Eyes_Look_Down_L] *= 1.5f;
    Expressions[FbExpression.Eyes_Look_Down_R] *= 1.5f;

    Expressions[FbExpression.Eyes_Look_Left_L] *= 0.85f;
    Expressions[FbExpression.Eyes_Look_Right_L] *= 0.85f;
    Expressions[FbExpression.Eyes_Look_Left_R] *= 0.85f;
    Expressions[FbExpression.Eyes_Look_Right_R] *= 0.85f;

    // Hack: turn rots to looks
    // Pitch = 29(left)-- > -29(right)
    // Yaw = -27(down)-- > 27(up)

    if (pitchL > 0)
    {
      Expressions[FbExpression.Eyes_Look_Left_L] = Math.Min(1, (float)(pitchL / 29.0)) * SranipalNormalizer;
      Expressions[FbExpression.Eyes_Look_Right_L] = 0;
    }
    else
    {
      Expressions[FbExpression.Eyes_Look_Left_L] = 0;
      Expressions[FbExpression.Eyes_Look_Right_L] = Math.Min(1, (float)(-pitchL / 29.0)) * SranipalNormalizer;
    }

    if (yawL > 0)
    {
      Expressions[FbExpression.Eyes_Look_Up_L] = Math.Min(1, (float)(yawL / 27.0)) * SranipalNormalizer;
      Expressions[FbExpression.Eyes_Look_Down_L] = 0;
    }
    else
    {
      Expressions[FbExpression.Eyes_Look_Up_L] = 0;
      Expressions[FbExpression.Eyes_Look_Down_L] = Math.Min(1, (float)(-yawL / 27.0)) * SranipalNormalizer;
    }


    if (pitchR > 0)
    {
      Expressions[FbExpression.Eyes_Look_Left_R] = Math.Min(1, (float)(pitchR / 29.0)) * SranipalNormalizer;
      Expressions[FbExpression.Eyes_Look_Right_R] = 0;
    }
    else
    {
      Expressions[FbExpression.Eyes_Look_Left_R] = 0;
      Expressions[FbExpression.Eyes_Look_Right_R] = Math.Min(1, (float)(-pitchR / 29.0)) * SranipalNormalizer;
    }

    if (yawR > 0)
    {
      Expressions[FbExpression.Eyes_Look_Up_R] = Math.Min(1, (float)(yawR / 27.0)) * SranipalNormalizer;
      Expressions[FbExpression.Eyes_Look_Down_R] = 0;
    }
    else
    {
      Expressions[FbExpression.Eyes_Look_Up_R] = 0;
      Expressions[FbExpression.Eyes_Look_Down_R] = Math.Min(1, (float)(-yawR / 27.0)) * SranipalNormalizer;
    }
  }
}