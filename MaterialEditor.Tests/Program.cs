using System;

namespace MaterialEditor.Tests
{
    internal static class Program
    {
        [STAThread]
        private static int Main()
        {
            try
            {
                AdvancedVariantTests.RunAll();
                AppearanceTests.RunAll();
                BulkMaterialEditSessionTests.RunAll();
                FieldOverwriteAdvancedTests.RunAll();
                ThemeTests.RunAll();
                Console.WriteLine("MaterialEditor.Tests passed.");
                return 0;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex.ToString());
                return 1;
            }
        }
    }
}
