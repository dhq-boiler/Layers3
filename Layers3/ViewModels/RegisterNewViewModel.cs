using Amazon;
using Amazon.Runtime;
using Amazon.S3;
using Prism.Mvvm;
using Prism.Services.Dialogs;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System.IO;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using Windows.Security.Credentials;
using DialogResult = Prism.Services.Dialogs.DialogResult;

namespace Layers3.ViewModels
{
    class RegisterNewViewModel : BindableBase, IDialogAware
    {
        private readonly CompositeDisposable _disposables = new();
        public ReactivePropertySlim<string> RegionStr { get; } = new();
        public ReadOnlyReactivePropertySlim<RegionEndpoint> RegionEndpoint { get; }
        public ReactivePropertySlim<string> BucketName { get; } = new();
        public ReactivePropertySlim<bool> A { get; } = new();
        public ReactivePropertySlim<bool> B { get; } = new();
        public ReactivePropertySlim<bool> C { get; } = new();
        public ReactivePropertySlim<bool> D { get; } = new();
        public ReactivePropertySlim<bool> E { get; } = new();
        public ReactivePropertySlim<bool> F { get; } = new();
        public ReactivePropertySlim<bool> G { get; } = new();
        public ReactivePropertySlim<bool> H { get; } = new();
        public ReactivePropertySlim<bool> I { get; } = new();
        public ReactivePropertySlim<bool> J { get; } = new();
        public ReactivePropertySlim<bool> K { get; } = new();
        public ReactivePropertySlim<bool> L { get; } = new();
        public ReactivePropertySlim<bool> M { get; } = new();
        public ReactivePropertySlim<bool> N { get; } = new();
        public ReactivePropertySlim<bool> O { get; } = new();
        public ReactivePropertySlim<bool> P { get; } = new();
        public ReactivePropertySlim<bool> Q { get; } = new();
        public ReactivePropertySlim<bool> R { get; } = new();
        public ReactivePropertySlim<bool> S { get; } = new();
        public ReactivePropertySlim<bool> T { get; } = new();
        public ReactivePropertySlim<bool> U { get; } = new();
        public ReactivePropertySlim<bool> V { get; } = new();
        public ReactivePropertySlim<bool> W { get; } = new();
        public ReactivePropertySlim<bool> X { get; } = new();
        public ReactivePropertySlim<bool> Y { get; } = new();
        public ReactivePropertySlim<bool> Z { get; } = new();
        public ReactivePropertySlim<bool> CanUseA { get; } = new();
        public ReactivePropertySlim<bool> CanUseB { get; } = new();
        public ReactivePropertySlim<bool> CanUseC { get; } = new();
        public ReactivePropertySlim<bool> CanUseD { get; } = new();
        public ReactivePropertySlim<bool> CanUseE { get; } = new();
        public ReactivePropertySlim<bool> CanUseF { get; } = new();
        public ReactivePropertySlim<bool> CanUseG { get; } = new();
        public ReactivePropertySlim<bool> CanUseH { get; } = new();
        public ReactivePropertySlim<bool> CanUseI { get; } = new();
        public ReactivePropertySlim<bool> CanUseJ { get; } = new();
        public ReactivePropertySlim<bool> CanUseK { get; } = new();
        public ReactivePropertySlim<bool> CanUseL { get; } = new();
        public ReactivePropertySlim<bool> CanUseM { get; } = new();
        public ReactivePropertySlim<bool> CanUseN { get; } = new();
        public ReactivePropertySlim<bool> CanUseO { get; } = new();
        public ReactivePropertySlim<bool> CanUseP { get; } = new();
        public ReactivePropertySlim<bool> CanUseQ { get; } = new();
        public ReactivePropertySlim<bool> CanUseR { get; } = new();
        public ReactivePropertySlim<bool> CanUseS { get; } = new();
        public ReactivePropertySlim<bool> CanUseT { get; } = new();
        public ReactivePropertySlim<bool> CanUseU { get; } = new();
        public ReactivePropertySlim<bool> CanUseV { get; } = new();
        public ReactivePropertySlim<bool> CanUseW { get; } = new();
        public ReactivePropertySlim<bool> CanUseX { get; } = new();
        public ReactivePropertySlim<bool> CanUseY { get; } = new();
        public ReactivePropertySlim<bool> CanUseZ { get; } = new();
        public ReactivePropertySlim<string> ApiKey { get; } = new();
        public ReactivePropertySlim<string> Secret { get; } = new();

        public ReactiveCommandSlim<object> OKCommand { get; }
        public ReactiveCommandSlim CancelCommand { get; }

        public RegisterNewViewModel()
        {
            A.Subscribe(flag =>
            {
                NotCheckedAll('A');
            }).AddTo(_disposables);
            B.Subscribe(flag =>
            {
                NotCheckedAll('B');
            }).AddTo(_disposables);

            C.Subscribe(flag =>
            {
                NotCheckedAll('C');
            }).AddTo(_disposables);
            D.Subscribe(flag =>
            {
                NotCheckedAll('D');
            }).AddTo(_disposables);
            E.Subscribe(flag =>
            {
                NotCheckedAll('E');
            }).AddTo(_disposables);
            F.Subscribe(flag =>
            {
                NotCheckedAll('F');
            }).AddTo(_disposables);
            G.Subscribe(flag =>
            {
                NotCheckedAll('G');
            }).AddTo(_disposables);
            H.Subscribe(flag =>
            {
                NotCheckedAll('H');
            }).AddTo(_disposables);
            I.Subscribe(flag =>
            {
                NotCheckedAll('I');
            }).AddTo(_disposables);
            J.Subscribe(flag =>
            {
                NotCheckedAll('J');
            }).AddTo(_disposables);
            K.Subscribe(flag =>
            {
                NotCheckedAll('K');
            }).AddTo(_disposables);
            L.Subscribe(flag =>
            {
                NotCheckedAll('L');
            }).AddTo(_disposables);
            M.Subscribe(flag =>
            {
                NotCheckedAll('M');
            }).AddTo(_disposables);
            N.Subscribe(flag =>
            {
                NotCheckedAll('N');
            }).AddTo(_disposables);
            O.Subscribe(flag =>
            {
                NotCheckedAll('O');
            }).AddTo(_disposables);
            P.Subscribe(flag =>
            {
                NotCheckedAll('P');
            }).AddTo(_disposables);
            Q.Subscribe(flag =>
            {
                NotCheckedAll('Q');
            }).AddTo(_disposables);
            R.Subscribe(flag =>
            {
                NotCheckedAll('R');
            }).AddTo(_disposables);
            S.Subscribe(flag =>
            {
                NotCheckedAll('S');
            }).AddTo(_disposables);
            T.Subscribe(flag =>
            {
                NotCheckedAll('T');
            }).AddTo(_disposables);
            U.Subscribe(flag =>
            {
                NotCheckedAll('U');
            }).AddTo(_disposables);
            V.Subscribe(flag =>
            {
                NotCheckedAll('V');
            }).AddTo(_disposables);
            W.Subscribe(flag =>
            {
                NotCheckedAll('W');
            }).AddTo(_disposables);
            X.Subscribe(flag =>
            {
                NotCheckedAll('X');
            }).AddTo(_disposables);
            Y.Subscribe(flag =>
            {
                NotCheckedAll('Y');
            }).AddTo(_disposables);
            Z.Subscribe(flag =>
            {
                NotCheckedAll('Z');
            }).AddTo(_disposables);

            var drives = DriveInfo.GetDrives()
                .Where(d => d.IsReady)
                .Select(d => d.Name.TrimEnd('\\'))
                .ToList();

            CanUseA.Value = true;
            CanUseB.Value = true;
            CanUseC.Value = true;
            CanUseD.Value = true;
            CanUseE.Value = true;
            CanUseF.Value = true;
            CanUseG.Value = true;
            CanUseH.Value = true;
            CanUseI.Value = true;
            CanUseJ.Value = true;
            CanUseK.Value = true;
            CanUseL.Value = true;
            CanUseM.Value = true;
            CanUseN.Value = true;
            CanUseO.Value = true;
            CanUseP.Value = true;
            CanUseQ.Value = true;
            CanUseR.Value = true;
            CanUseS.Value = true;
            CanUseT.Value = true;
            CanUseU.Value = true;
            CanUseV.Value = true;
            CanUseW.Value = true;
            CanUseX.Value = true;
            CanUseY.Value = true;
            CanUseZ.Value = true;

            foreach (var drive in drives)
            {
                switch (drive)
                {
                    case "A:":
                        CanUseA.Value = false;
                        break;
                    case "B:":
                        CanUseB.Value = false;
                        break;
                    case "C:":
                        CanUseC.Value = false;
                        break;
                    case "D:":
                        CanUseD.Value = false;
                        break;
                    case "E:":
                        CanUseE.Value = false;
                        break;
                    case "F:":
                        CanUseF.Value = false;
                        break;
                    case "G:":
                        CanUseG.Value = false;
                        break;
                    case "H:":
                        CanUseH.Value = false;
                        break;
                    case "I:":
                        CanUseI.Value = false;
                        break;
                    case "J:":
                        CanUseJ.Value = false;
                        break;
                    case "K:":
                        CanUseK.Value = false;
                        break;
                    case "L:":
                        CanUseL.Value = false;
                        break;
                    case "M:":
                        CanUseM.Value = false;
                        break;
                    case "N:":
                        CanUseN.Value = false;
                        break;
                    case "O:":
                        CanUseO.Value = false;
                        break;
                    case "P:":
                        CanUseP.Value = false;
                        break;
                    case "Q:":
                        CanUseQ.Value = false;
                        break;
                    case "R:":
                        CanUseR.Value = false;
                        break;
                    case "S:":
                        CanUseS.Value = false;
                        break;
                    case "T:":
                        CanUseT.Value = false;
                        break;
                    case "U:":
                        CanUseU.Value = false;
                        break;
                    case "V:":
                        CanUseV.Value = false;
                        break;
                    case "W:":
                        CanUseW.Value = false;
                        break;
                    case "X:":
                        CanUseX.Value = false;
                        break;
                    case "Y:":
                        CanUseY.Value = false;
                        break;
                    case "Z:":
                        CanUseZ.Value = false;
                        break;
                }
            }


            RegionEndpoint = RegionStr.Select(str =>
            {
                switch (str)
                {
                    case "us-east-1":
                        return Amazon.RegionEndpoint.USEast1;
                    case "us-east-2":
                        return Amazon.RegionEndpoint.USEast2;
                    case "us-west-1":
                        return Amazon.RegionEndpoint.USWest1;
                    case "us-west-2":
                        return Amazon.RegionEndpoint.USWest2;
                    case "af-south-1":
                        return Amazon.RegionEndpoint.AFSouth1;
                    case "ap-northeast-1":
                        return Amazon.RegionEndpoint.APNortheast1;
                    case "ap-northeast-2":
                        return Amazon.RegionEndpoint.APNortheast2;
                    case "ap-northeast-3":
                        return Amazon.RegionEndpoint.APNortheast3;
                    case "ap-east-1":
                        return Amazon.RegionEndpoint.APEast1;
                    case "ap-south-1":
                        return Amazon.RegionEndpoint.APSouth1;
                    case "ap-south-2":
                        return Amazon.RegionEndpoint.APSouth2;
                    case "ap-southeast-1":
                        return Amazon.RegionEndpoint.APSoutheast1;
                    case "ap-southeast-2":
                        return Amazon.RegionEndpoint.APSoutheast2;
                    case "ap-southeast-3":
                        return Amazon.RegionEndpoint.APSoutheast3;
                    case "ap-southeast-4":
                        return Amazon.RegionEndpoint.APSoutheast4;
                    case "ca-central-1":
                        return Amazon.RegionEndpoint.CACentral1;
                    case "ca-west-1":
                        return Amazon.RegionEndpoint.CAWest1;
                    case "eu-central-1":
                        return Amazon.RegionEndpoint.EUCentral1;
                    case "eu-central-2":
                        return Amazon.RegionEndpoint.EUCentral2;
                    case "eu-west-1":
                        return Amazon.RegionEndpoint.EUWest1;
                    case "eu-west-2":
                        return Amazon.RegionEndpoint.EUWest2;
                    case "eu-west-3":
                        return Amazon.RegionEndpoint.EUWest3;
                    case "eu-south-1":
                        return Amazon.RegionEndpoint.EUSouth1;
                    case "eu-south-2":
                        return Amazon.RegionEndpoint.EUSouth2;
                    case "eu-north-1":
                        return Amazon.RegionEndpoint.EUNorth1;
                    case "il-central-1":
                        return Amazon.RegionEndpoint.ILCentral1;
                    case "me-south-1":
                        return Amazon.RegionEndpoint.MECentral1;
                    case "me-central-1":
                        return Amazon.RegionEndpoint.MESouth1;
                    case "sa-east-1":
                        return Amazon.RegionEndpoint.SAEast1;
                    default:
                        return null;
                }
            }).ToReadOnlyReactivePropertySlim();

            var apiKeyIsNotEmpty = ApiKey.Select(x => !string.IsNullOrEmpty(x)).ToReadOnlyReactivePropertySlim();
            var secretIsNotEmpty = Secret.Select(x => !string.IsNullOrEmpty(x)).ToReadOnlyReactivePropertySlim();
            var RegionIsNotSelected = RegionEndpoint.Select(x => x is not null).ToReadOnlyReactivePropertySlim();
            var BacketNameIsNotEmpty =
                BucketName.Select(x => !string.IsNullOrEmpty(x)).ToReadOnlyReactivePropertySlim();
            var SingleDriveLetterButtonIsPressed = new[] { A, B, C, D, E, F, G, H, I, J, K, L, M, N, O, P, Q, R, S, T, U, V, W, X, Y, Z }
                                                                    .CombineLatest()
                                                                    .Select(x => x.Count(y => y) == 1)
                                                                    .ToReadOnlyReactivePropertySlim();

            OKCommand = new[] { apiKeyIsNotEmpty, secretIsNotEmpty, RegionIsNotSelected, BacketNameIsNotEmpty, SingleDriveLetterButtonIsPressed }
                .CombineLatestValuesAreAllTrue()
                .ToReactiveCommandSlim()
                .WithSubscribe(_ =>
                {
                    var parameters = new DialogParameters();
                    parameters.Add("region", RegionEndpoint.Value.SystemName);
                    parameters.Add("bucketName", BucketName.Value);
                    parameters.Add("mountPoint", GetSelectedDriveLetter());
                    parameters.Add("client", new AmazonS3Client(new BasicAWSCredentials(ApiKey.Value, Secret.Value), RegionEndpoint.Value));

                    var myVault = new PasswordVault();
                    myVault.Add(new PasswordCredential($"{RegionEndpoint.Value.DisplayName}/{BucketName.Value}/apikey", Environment.UserName, ApiKey.Value));
                    myVault.Add(new PasswordCredential($"{RegionEndpoint.Value.DisplayName}/{BucketName.Value}/secret", Environment.UserName, Secret.Value));

                    var result = new DialogResult(ButtonResult.OK, parameters);
                    RequestClose?.Invoke(result);
                }).AddTo(_disposables);
            CancelCommand = new ReactiveCommandSlim().WithSubscribe(() =>
            {
                var result = new DialogResult(ButtonResult.Cancel);
                RequestClose?.Invoke(result);
            }).AddTo(_disposables);
        }

        private string GetSelectedDriveLetter()
        {
            if (A.Value)
                return "A";
            if (B.Value)
                return "B";
            if (C.Value)
                return "C";
            if (D.Value)
                return "D";
            if (E.Value)
                return "E";
            if (F.Value)
                return "F";
            if (G.Value)
                return "G";
            if (H.Value)
                return "H";
            if (I.Value)
                return "I";
            if (J.Value)
                return "J";
            if (K.Value)
                return "K";
            if (L.Value)
                return "L";
            if (M.Value)
                return "M";
            if (N.Value)
                return "N";
            if (O.Value)
                return "O";
            if (P.Value)
                return "P";
            if (Q.Value)
                return "Q";
            if (R.Value)
                return "R";
            if (S.Value)
                return "S";
            if (T.Value)
                return "T";
            if (U.Value)
                return "U";
            if (V.Value)
                return "V";
            if (W.Value)
                return "W";
            if (X.Value)
                return "X";
            if (Y.Value)
                return "Y";
            if (Z.Value)
                return "Z";

            return String.Empty;
        }

        private bool isIn = false;

        public void NotCheckedAll(char target)
        {
            if (isIn)
                return;

            isIn = true;

            if (target != 'A')
                A.Value = false;
            if (target != 'B')
                B.Value = false;
            if (target != 'C')
                C.Value = false;
            if (target != 'D')
                D.Value = false;
            if (target != 'E')
                E.Value = false;
            if (target != 'F')
                F.Value = false;
            if (target != 'G')
                G.Value = false;
            if (target != 'H')
                H.Value = false;
            if (target != 'I')
                I.Value = false;
            if (target != 'J')
                J.Value = false;
            if (target != 'K')
                K.Value = false;
            if (target != 'L')
                L.Value = false;
            if (target != 'M')
                M.Value = false;
            if (target != 'N')
                N.Value = false;
            if (target != 'O')
                O.Value = false;
            if (target != 'P')
                P.Value = false;
            if (target != 'Q')
                Q.Value = false;
            if (target != 'R')
                R.Value = false;
            if (target != 'S')
                S.Value = false;
            if (target != 'T')
                T.Value = false;
            if (target != 'U')
                U.Value = false;
            if (target != 'V')
                V.Value = false;
            if (target != 'W')
                W.Value = false;
            if (target != 'X')
                X.Value = false;
            if (target != 'Y')
                Y.Value = false;
            if (target != 'Z')
                Z.Value = false;
            isIn = false;
        }

        public bool CanCloseDialog() => true;

        public void OnDialogClosed()
        {
        }

        public void OnDialogOpened(IDialogParameters parameters)
        {
        }

        public string Title => "S3 バケットの登録";
        public event Action<IDialogResult>? RequestClose;
    }
}
