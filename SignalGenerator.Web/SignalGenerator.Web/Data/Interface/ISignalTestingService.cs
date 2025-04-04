
    namespace SignalGenerator.Web.Data.Interface
    {
        using SignalGenerator.Data.Models;
        using System.Collections.Generic;
        using System.Threading.Tasks;

        public interface ISignalTestingService
        {
            /// <summary>
            /// تست ارسال سیگنال بر اساس پیکربندی داده‌شده.
            /// </summary>
            /// <param name="config">پیکربندی سیگنال</param>
            /// <returns>نتیجه تست</returns>
            Task<TestResult> TestSignalTransmissionAsync(SignalData config);

            /// <summary>
            /// دریافت وضعیت فعلی تست.
            /// </summary>
            /// <returns>وضعیت تست</returns>
            Task<string> GetTestStatusAsync();

            /// <summary>
            /// دریافت لیست خطاهای ثبت‌شده.
            /// </summary>
            /// <returns>لیست خطاها</returns>
            Task<List<string>> GetErrorsAsync();

            /// <summary>
            /// اضافه کردن یک پیام خطا.
            /// </summary>
            /// <param name="errorMessage">پیام خطا</param>
            void AddError(string errorMessage);
        }
    }


