using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Linq;

namespace All_Hz_Generator
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private string OutputFilePath = @".\output.txt";

        // 入力をリストに変換（区切りは改行、環境依存なし）
        private double[] ParseInputList(string input)
        {
            return input
                .Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries)
                .Select(s => double.Parse(s.Trim()))
                .ToArray();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // 入力の取得
                string freqText = new TextRange(freqinput.Document.ContentStart, freqinput.Document.ContentEnd).Text;
                string beatText = new TextRange(beatinput.Document.ContentStart, beatinput.Document.ContentEnd).Text;

                // 数値の読み取りと変換
                double songBPM = double.Parse(songbpminput.Text);
                double restSeconds = double.Parse(restinput.Text);

                double[] freqs = ParseInputList(freqText);
                double[] beats = ParseInputList(beatText);

                if (freqs.Length != beats.Length)
                    throw new InvalidOperationException("周波数と拍数のリストの長さは一致していなければなりません。");

                int count = freqs.Length;

                double[] noteDurations = new double[count]; // タイル数
                double[] noteBPMs = new double[count];      // 各音のBPM
                double[] restBPMs = new double[count];      // 各休符BPM

                for (int i = 0; i < count; i++)
                {
                    noteDurations[i] = freqs[i] * (60 / songBPM) * beats[i];
                    noteBPMs[i] = freqs[i] * 60;

                    double tileFraction = noteDurations[i] % 1;
                    noteDurations[i] = Math.Floor(noteDurations[i]);
                    restBPMs[i] = 60 / (restSeconds + (tileFraction * 60 / noteBPMs[i]));

                    // NOTE: restBPMが倍になるかどうかは、拍数とHzの積に小数が含まれるかによる。
                    // 目的に応じてMath.RoundやMath.Floorを適用する必要があるかも。
                }

                // ファイルの準備
                if (!File.Exists(OutputFilePath))
                {
                    using (File.Create(OutputFilePath)) { }
                }
                else if (new FileInfo(OutputFilePath).Length > 0)
                {
                    if (!ConfirmIfOverwrite(OutputFilePath))
                        return;

                    File.WriteAllText(OutputFilePath, string.Empty);
                }

                WriteToFile(OutputFilePath, freqs, beats, noteDurations, noteBPMs, restBPMs);

                MessageBox.Show("出力が完了しました。", "完了", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (FormatException)
            {
                MessageBox.Show("数値の形式が正しくありません。入力を確認してください。", "入力エラー", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (OverflowException)
            {
                MessageBox.Show("非常に大きな数値が入力されました。入力を確認してください。", "入力エラー", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // 出力ファイルに書き込む
        private void WriteToFile(string path, double[] freqs, double[] beats, double[] durations, double[] noteBPMs, double[] restBPMs)
        {
            using StreamWriter sw = new(path, true, Encoding.UTF8);
            for (int i = 0; i < freqs.Length; i++)
            {
                sw.WriteLine(GetFormattedText(freqs[i], beats[i], durations[i], noteBPMs[i], restBPMs[i]));
            }
        }

        // ファイル上書きの確認
        private bool ConfirmIfOverwrite(string path)
        {
            var result = MessageBox.Show("出力先のファイルに内容が存在します。上書きしますか？",
                                         "確認", 
                                         MessageBoxButton.YesNo, 
                                         MessageBoxImage.Warning
                                         );
            return result == MessageBoxResult.Yes;
        }

        // 整形済みテキスト出力
        private string GetFormattedText(double hz, double beats, double tiles, double bpm, double restBpm) => $"{hz,6:F2}Hz\t{beats,4:F1}拍\t{tiles,8:F0}タイル\t{bpm,7:F2} BPM\t{restBpm,7:F2} 休符BPM";
    }
}
