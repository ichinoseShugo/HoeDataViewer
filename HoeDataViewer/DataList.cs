using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HoeDataViewer
{
    public class DataList
    {
        /// <summary>
        /// 読み込んだファイル内の各列についている名前の配列
        /// </summary>
        public List<string> DataNames = new List<string>();
        /// <summary>
        /// 列(データ)の名前をkeyとしてvalueをdouble配列としたHash
        /// </summary>
        private Hashtable NameToData = new Hashtable();
        /// <summary>
        /// 列(データ)の名前をkeyとしてvalueを特徴点としたHash
        /// </summary>
        private Hashtable NameToPrimitive = new Hashtable();
        /// <summary>
        /// 列(データ)の名前をkeyとしてvalueを列の最大値と最小値([0]:max [1]:minのdouble配列)としたHash
        /// </summary>
        private Hashtable NameToMaxMin = new Hashtable();

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="directoryPath"></param>
        public DataList(string directoryPath)
        {
            //ディレクトリ以下の全てのcsvファイルのパスを取得
            string[] files = Directory.GetFiles(directoryPath, "*.csv", SearchOption.AllDirectories);
            foreach(var file in files) CreateDataList(file);
            CreateSpeedList();
        }

        /// <summary>
        /// ファイルからデータリストを作成
        /// </summary>
        /// <param name="files"></param>
        private void CreateDataList(string file)
        {
            using (StreamReader sr = new StreamReader(file, Encoding.UTF8))
            {
                string line = sr.ReadLine();//ファイルのヘッダを読み込む
                string[] RowNames = line.Split(',');//列の名前
                DataNames.AddRange(RowNames);//DataNamesの追加

                //各列に対応するリストの作成(要素は[0]:時間　[1]:データとなるdouble配列)
                List<double[]>[] RowLists = new List<double[]>[RowNames.Length];
                //列の最大値最小値を記録するリストの用意(要素は[0]:最大値　[1]:最小値)
                List<double[]> MaxMin = new List<double[]>();

                //リストの初期化
                for (int i = 0; i < RowLists.Length; i++)
                {
                    RowLists[i] = new List<double[]>();
                    MaxMin.Add(new double[] { -999999, 999999 });
                }

                //データポイント用double配列を用意(要素は[0]:時間　[1]:データ)
                int lineCount = 0;//time列のX軸用行数カウント
                while ((line = sr.ReadLine()) != null)
                {
                    String[] token = line.Split(',');//token[0]=time， token[n]=データ(n>0)

                    //time列のdouble配列をリストに追加する
                    double[] TimeAndData = new double[2];
                    TimeAndData[0] = lineCount++;//X軸は1刻み
                    TimeAndData[1] = Double.Parse(token[0]);//Y軸にtime
                    RowLists[0].Add((double[])TimeAndData.Clone());//列リストの0にtime列を追加

                    //最大値最小値を更新
                    if (MaxMin[0][0] < TimeAndData[1]) MaxMin[0][0] = TimeAndData[1];
                    if (MaxMin[0][1] > TimeAndData[1]) MaxMin[0][1] = TimeAndData[1];

                    //time以外の列のdouble配列をリストに追加
                    for (int j = 1; j < token.Length; j++)//token[0]はtimeの値なのでj=1から始める
                    {
                        TimeAndData[0] = Double.Parse(token[0]);//X軸はtoken[0](time)
                        TimeAndData[1] = Double.Parse(token[j]);//Y軸にその列のセルデータ
                        RowLists[j].Add((double[])TimeAndData.Clone());//double配列を列リストに追加

                        //最大値最小値を更新
                        if (MaxMin[j][0] < TimeAndData[1]) MaxMin[j][0] = TimeAndData[1];
                        if (MaxMin[j][1] > TimeAndData[1]) MaxMin[j][1] = TimeAndData[1];
                    }
                }

                //hashによる名前(key)とデータ配列(value)の対応付け
                for (int k = 0; k < RowLists.Length; k++)
                {
                    NameToData.Add(RowNames[k], RowLists[k]);
                    NameToMaxMin.Add(RowNames[k], MaxMin[k]);
                }
            }
        }

        private void CreateSpeedList()
        {
            foreach (string joint in JointNames.Joints)
            {
                List<double[]> positionX = GetDataList(joint + "_X");
                List<double[]> positionY = GetDataList(joint + "_Y");
                List<double[]> positionZ = GetDataList(joint + "_Z");
                List<double[]> speed = new List<double[]> { new double[] { positionX[0][0], 0 } };
                double[] MaxMin = new double[] { -99999, 0 };

                for (int i = 1; i < positionX.Count; i++)
                {
                    double s = (positionX[i][0] - positionX[i - 1][0]) / 1000; //sec
                    if (s == 0)
                    {
                        speed.Add(new double[] { positionX[i-1][0], speed[speed.Count-1][1]});
                        continue;
                    }
                    double m = Math.Sqrt(
                        Math.Pow(positionX[i][1] - positionX[i - 1][1], 2)
                        + Math.Pow(positionY[i][1] - positionX[i - 1][1], 2)
                        + Math.Pow(positionY[i][1] - positionX[i - 1][1], 2)); //m
                    double v = m / s;
                    //if (joint == "HandLeft") Console.WriteLine(positionX[i][0]);
                    /*
                    speed.Add(new double[] { positionX[i][0], v });
                    if (MaxMin[0] < v) MaxMin[0] = v;
                    if (MaxMin[1] > v) MaxMin[1] = v;
                    */
                    speed.Add(new double[] { positionX[i][0], m });
                    if (MaxMin[0] < m) MaxMin[0] = m;
                    if (MaxMin[1] > m) MaxMin[1] = m;
                }
                //Console.WriteLine("Max:" + MaxMin[0] + " Min:" + MaxMin[1]);
                DetectFeatures(joint, speed);

                DataNames.Add(joint + "_Speed");
                NameToData[joint + "_Speed"] = speed;
                NameToMaxMin[joint + "_Speed"] = MaxMin;
            }
        }

        private void DetectFeatures(string joint, List<double[]> speedList)
        {
            List<double[]> DataPointsList = new List<double[]>();
            DataPointsList.Add(new double[] { speedList[0][1], 0 });
            for (int i=1; i<speedList.Count-1; i++)
            {
                if (speedList[i - 1][1] > speedList[i][1] && speedList[i][1] < speedList[i + 1][1])
                {
                    DataPointsList.Add(new double[] { speedList[i][0], speedList[i][1] });
                }
                else
                {
                    DataPointsList.Add(new double[] { speedList[i][0], 0 });
                }
                //if (joint == "HandLeft") Console.WriteLine(speedList[i][1]);
            }
            DataPointsList.Add(new double[] { speedList[speedList.Count-1][1], 0 });
            NameToPrimitive[joint + "_Primitive"] = DataPointsList;
        }

        /// <summary>
        /// データの名前からdouble配列リスト([0]:X軸 [1]:Y軸)を取得
        /// </summary>
        /// <param name="DataName"></param>
        /// <returns></returns>
        public List<double[]> GetDataList(string DataName)
        {
            return (List<double[]>)NameToData[DataName];
        }

        /// <summary>
        /// データの名前からdouble配列リスト([0]:X軸 [1]:Y軸)を取得
        /// </summary>
        /// <param name="DataName"></param>
        /// <returns></returns>
        public List<double[]> GetPrimitiveList(string DataName)
        {
            return (List<double[]>)NameToPrimitive[DataName];
        }

        /// <summary>
        /// データの名前から、そのデータ列の最大値を返す
        /// </summary>
        /// <param name="DataName"></param>
        /// <returns></returns>
        public double GetMax(string DataName)
        {
            return ((double[])NameToMaxMin[DataName])[0];
        }

        /// <summary>
        /// データの名前から、そのデータ列の最大値を返す
        /// </summary>
        /// <param name="DataName"></param>
        /// <returns></returns>
        public double GetMin(string DataName)
        {
            return ((double[])NameToMaxMin[DataName])[1];
        }
    }
}