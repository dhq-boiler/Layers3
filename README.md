# Layers3 - Amazon S3 File System on Windows - 

[![License](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE)

## Description / 説明

Layers3は Amazon S3 のバケットを Windows にマウントして、エクスプローラー上でバケットのコンテンツを操作できるようにします。

Layers3 mounts Amazon S3 buckets on Windows so that the bucket contents can be manipulated on Explorer.

## Installation / インストール

Releases から最新の Setup.exe をダウンロード・実行して、インストールウィザードに従いインストールしてください。

Download and run the latest Setup.exe from Releases and follow the installation wizard.

## Usage / 使い方

インストールしたら、デスクトップに作成された "Layers3" ショートカットをダブルクリックして、Layers3を起動します。

Once installed, launch Layers3 by double-clicking the "Layers3" shortcut created on the desktop.

Layers3は起動直後最小化された後、登録されているドライブをすべてマウントします。

Layers3 is minimized immediately after startup and then mounts all registered drives.

メイン画面を表示するにはタスクトレイからLayers3のアイコンを右クリックし、「Layers3」をクリックします。

To display the main screen, right-click the Layers3 icon from the task tray and click "Layers3".

## Register S3 bucket / S3バケットの登録

メイン画面のメニュー＞S3バケット＞登録 をクリックします。

Click Menu > S3 Bucket > Register on the main screen.

S3バケットの登録画面が開き、ここで Amazon S3のバケットをマウントするための情報を入力します。

The S3 bucket registration screen opens, where you enter information to mount the Amazon S3 bucket.

Amazon S3のバケットをマウントするのに必要な情報は以下の通りです:

The information required to mount an Amazon S3 bucket is as follows:

* AWS の適切な許可ポリシーが設定された IAMユーザー のアクセスキー（APIキーとシークレット）/ IAM user access key (API key and secret) with appropriate AWS permission policy set ※1
* Amazon S3 バケットのリージョン / Amazon S3 bucket region 
* Amazon S3 バケット名 / Amazon S3 bucket name
* Windowsシステムのマウントポジション（マウントするドライブのドライブレター） / Windows system mount position (drive letter of the drive to mount)

OKボタンを押すと、S3バケットが登録されます。

Pressing the OK button registers the S3 bucket.

![alt text](assets/img/image-25.png)

### ※1 の取得方法

AWS のコンソールのホームからIAMにアクセスします。

![コンソールのホーム](assets/img/image-1.png)

または、AWSのコンソールのサービスから「セキュリティ、ID、およびコンプライアンス」＞「IAM」をクリックします。

![コンソールのホーム＞サービス](assets/img/image-2.png)

ユーザーの作成ボタンをクリックします。

![IAM＞ユーザーの作成](assets/img/image-3.png)

Layers3で使用するユーザー名を入力します。

※Layers3の使用中に何かセキュリティ的な問題が生じた時に問題を切り分けるためにユーザーを作成します。

![IAM＞ユーザーの作成＞ユーザー名](assets/img/image-4.png)

「ユーザーをグループに追加」をクリック、ユーザーグループから「グループを作成」をクリックします。

![IAM＞ユーザーの作成＞許可を設定](assets/img/image-6.png)

ユーザーグループ名を入力します。

許可ポリシーから「ポリシーの作成」をクリックします。

![alt text](assets/img/image-8.png)

ポリシーエディターが開かれるので、JSONモードに切り替えます。

![alt text](assets/img/image-10.png)

![alt text](assets/img/image-11.png)

Layers3で、「S3バケットの登録」ダイアログを開きます。（メインメニュー＞S3バケット＞登録）

自らが所有するバケットのバケット名のみを入力して、「推奨されたポリシーを表示」をクリックします。

![alt text](assets/img/image-12.png)

表示されたJSONテキストをコピーします。

※このJSONテキストはLayers3でS3バケットをマウントするのに必要なポリシーを表したものです。

![alt text](assets/img/image-13.png)

ポリシーエディタに貼り付けます。「次へ」をクリックします。

![alt text](assets/img/image-14.png)

ポリシー名を入力します。

「ポリシーの作成」をクリックします。

![alt text](assets/img/image-16.png)

別タブで開かれていたはずのユーザーの作成の「ユーザーグループを作成」モーダル画面で作成したポリシーにチェックを入れて、「ユーザーグループを作成」ボタンをクリックします。

![alt text](assets/img/image-17.png)

「許可を設定」画面で、作成したユーザーグループにチェックを入れ、「次へ」をクリックします。

![alt text](assets/img/image-18.png)

「確認して作成」画面で、「ユーザーの作成」ボタンをクリックします。

![alt text](assets/img/image-19.png)

作成されたユーザーをクリックします。

![alt text](assets/img/image-20.png)

画面中段の「セキュリティ認証情報」タブを開きます。

アクセスキー＞アクセスキーを作成をクリックします。

![alt text](assets/img/image-21.png)

「コマンドラインインターフェース(CLI)」を選択します。

「上記のレコメンデーションを理解し、アクセスキーを作成します。」にチェックを入れ、「次へ」ボタンをクリックします。

![alt text](assets/img/image-22.png)

「アクセスキーを作成」ボタンをクリックします。

![alt text](assets/img/image-23.png)

アクセスキーが表示され、シークレットはマスクされています。

シークレットのマスクは「表示」リンクをクリックすることで外すことができます。

アクセスキーとシークレットをそれぞれ控えておきます。

![alt text](assets/img/image-24.png)

Layers3のS3バケットの登録画面に戻り、リージョン、マウントポイント、APIキー（ここにアクセスキーを入力）、シークレットを入力してOKボタンをクリックします。

![alt text](assets/img/image-25.png)

これでS3バケットの登録は完了です！

## Registered drives / 登録されているドライブ

<span style="color: lime;">■</span>が表示されているドライブはマウント中の状態です。
<span style="color: red;">■</span>が表示されているドライブはマウントされていない状態です。

Drives marked with <span style="color: lime;">■</span> are currently mounted. Drives marked with <span style="color: red;">■</span> are unmounted.

![alt text](assets/img/image.png)

### 手動マウント

![alt text](assets/img/image-9.png)

### 手動アンマウント

![alt text](assets/img/image-5.png)

## Unregister S3 bucket / S3バケットの登録解除

メイン画面で登録解除するドライブを選択し、メイン画面のメニュー＞S3バケット＞登録解除　をクリックします。

On the main screen, select the drive to be unregistered and click Menu > S3 Bucket > Unregister on the main screen.

## Contributing / 貢献

プルリクエスト歓迎します！！！

Pull requests are welcome!

## License / ライセンス

このプロジェクトのライセンスは[MIT License](LICENSE)です。

This project is licensed under the [MIT License](LICENSE).