using FileProcessor;
using FileProcessor.Model;
using SharpCompress.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TextReverserWPF.Services
{
    public class ReverserDataService
    {
        public ReverserDataService() { }

        ReverseData reverserData;

        public async void StartProcessing(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(reverserData.ReverseType) || string.IsNullOrEmpty(reverserData.InputFile) || string.IsNullOrEmpty(reverserData.OutputFile))
            {
                string errorMessage = "Missing information! Please provide all required fields.";
                //await DisplayAlert("Alert", errorMessage, "OK");
                return;
            }

            // Start a new thread or use a Task to call the ProcessFile method
            Task.Run(() => FileProcessorWorker.ProcessFile(reverserData));
        }
    }
}
