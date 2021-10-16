namespace TestGenerator
{
    public class Programm
    {
        public static void Main(string[] args)
        {
            var asd = new PropChanged();
            asd.MyProperty = 123;
            asd.MyProperty2 = 123;
            asd.Different = 123;
        }
    }
}