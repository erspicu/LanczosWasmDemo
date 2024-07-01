using System.Collections;
using System.Drawing;
using System.Runtime.InteropServices.JavaScript;
using System.Diagnostics;


using LanczosAlg;

namespace Greeter;

public static partial class JSInteropCallsContainer
{
    // this simple static method is exported to JavaScript
    // via WebAssembly
    [JSExport]
    public static string Greet(params string[] names)
    {
        var resultStr = string.Join(", ", names);

        // return a string greeting comma separated names passed to it
        // e.g. if the array of names contains two names "Joe" and "Jack"
        // then the resulting string will be "Hello Joe, Jack!!!".
        return $"Hello zzzzz {resultStr}!!!";
    }


    [JSExport]
    public static string LanczosWasm(string a)
    {
        //Bitmap bitmap = null ;  
        //Lanczos tool = new Lanczos();
        //Bitmap x = tool.ResizeLanczos(800, 600);
        //a = a + "///";

        return "zzz";// tool.call_test(a);
    }

    [JSExport]
    public static byte [] read_array( byte [] image_bytes , int org_w, int org_h, int new_w, int new_h)
    {
        //https://learn.microsoft.com/zh-tw/aspnet/core/client-side/dotnet-interop?view=aspnetcore-8.0

        
        Stopwatch st = new Stopwatch();
        st.Restart();
        Lanczos tool = new Lanczos( image_bytes,  org_w, org_h );
        byte[] result = tool.ResizeLanczos(new_w, new_h);
        st.Stop();
        Console.WriteLine( "cost time :"　+ st.ElapsedMilliseconds );


        //byte[] result = new byte[2] { 1, 1 };

        return result;
    }


}

