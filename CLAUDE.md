# プロジェクト概要
- Unityを用いたサウンド実装の実験プロジェクト（Unity 6000.3.8f1）
- 機能ごとにフォルダを分けて管理している

# 実装前に確認すること
- READMEを読んでプロジェクト概要を把握してください。
- 対象フォルダ内の既存コードを読んで、設計方針を理解してから実装に着手してください。

# フォルダ構成（Assets/）
- `ProceduralAudio/` - プロシージャルサウンド生成（バイクアッドフィルタ、レゾネーター等）
- `Groove/` - ビート/リズム同期システム（GrooveConductor, GrooveReactor）
- `GrooveWithTool/` - エディタツール付きグルーヴシステム（Runtime/ + Editor/）
- `3DSound/` - 3Dサウンド（遮蔽・回折モジュール付きカスタムAudioSource）

# コード規則
- Microsoftのコーディング規則に準拠する
- 変数宣言では `var` を利用してください。
- `{}` は省略しないでください。

# Unity環境
- Unity 6000.3.8f1
- URP（Universal Render Pipeline）使用
- 新Input System使用
