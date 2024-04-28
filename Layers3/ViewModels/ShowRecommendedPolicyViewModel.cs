using Prism.Mvvm;
using Prism.Services.Dialogs;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System.Reactive.Disposables;
using DialogResult = Prism.Services.Dialogs.DialogResult;

namespace Layers3.ViewModels
{
    class ShowRecommendedPolicyViewModel : BindableBase , IDialogAware
    {
        private readonly CompositeDisposable _disposables = new();
        public ReactivePropertySlim<string> Json { get; } = new();

        public ReactiveCommandSlim OKCommand { get; }

        public ShowRecommendedPolicyViewModel()
        {
            OKCommand = new ReactiveCommandSlim().WithSubscribe(() =>
            {
                RequestClose.Invoke(new DialogResult(ButtonResult.OK));
            }).AddTo(_disposables);
        }

        public bool CanCloseDialog() => true;

        public void OnDialogClosed()
        {
        }

        public void OnDialogOpened(IDialogParameters parameters)
        {
            var bucketName = parameters.GetValue<string>("bucketName");
            Json.Value =
                @"{
  ""Version"": ""2012-10-17"",
  ""Statement"": [
    {
      ""Effect"": ""Allow"",
      ""Action"": [
        ""s3:PutObject"",
        ""s3:GetObject"", 
        ""s3:ListBucket"",
        ""s3:DeleteObject""
      ],
      ""Resource"": [
        ""arn:aws:s3:::bucketName"",
        ""arn:aws:s3:::bucketName/*""
      ]
    }
  ]
}";
            Json.Value = Json.Value.Replace("bucketName", bucketName);
        }

        public string Title => "推奨されたポリシー";
        public event Action<IDialogResult>? RequestClose;
    }
}
