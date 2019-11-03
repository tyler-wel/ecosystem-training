using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class FalloffGenerator
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="size">Map size</param>
    /// <returns>A a float array falloff map (size x size)</returns>
    public static float[,] GenerateFalloffMap(int size) {
        float[,] map = new float[size, size];

        for (int i = 0; i < size; i++) {
            for (int j = 0; j < size; j++) {
                // i / size gives value 0~1
                // multiply by 2 and subtract by 1 to get -1 ~ 1
                float x = i / (float)size * 2 - 1;
                float y = j / (float)size * 2 - 1;

                // Which is closer to an edge
                float value = Mathf.Max(Mathf.Abs(x), Mathf.Abs(y));
                map[i,j] = Evaluate(value);
            }
        }

        return map;
    }

    static float Evaluate(float value) {
        float a = 3;
        float b = 2.2f;

        return Mathf.Pow(value, a) / (Mathf.Pow(value, a) + Mathf.Pow(b -  b * value, a));
    }
}
