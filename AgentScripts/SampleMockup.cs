using UnityEngine;
public static class SampleMockup {
  public static string Run() {
    var bytes = System.IO.File.ReadAllBytes("References/UI_Layout.png");
    var t = new Texture2D(2,2); t.LoadImage(bytes);
    int[][] pts = { new[]{60,60}, new[]{815,375}, new[]{400,1000}, new[]{1000,90}, new[]{965,1000}, new[]{960,1600}, new[]{285,1775}, new[]{500,900}, new[]{20,1900} };
    string[] names = { "settings","coin","table","bonus","power","shoot","shots","felt2","background" };
    var sb = new System.Text.StringBuilder($"{t.width}x{t.height} ");
    for (int i=0;i<pts.Length;i++){ var c=(Color32)t.GetPixel(pts[i][0], t.height-1-pts[i][1]); sb.Append($"{names[i]}=#{c.r:X2}{c.g:X2}{c.b:X2} "); }
    Object.DestroyImmediate(t);
    return sb.ToString();
  }
}
