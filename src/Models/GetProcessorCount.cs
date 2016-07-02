using System;

namespace AperyGenerateTeacherGUI.Models
{
    static class GetProcessorCount
    {
        public static int Count()
        {
            return Environment.ProcessorCount;
        }
    }
}
