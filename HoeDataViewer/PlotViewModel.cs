using System;
using System.Collections;
using System.Collections.Generic;

using OxyPlot;
using OxyPlot.Series;

namespace HoeDataViewer
{
    class PlotViewModel
    {
        DataList MyDataList;
        public PlotModel DataModel;
        public PlotModel PrimitiveModel;

        /// <summary>
        /// 読み込んだファイル内の各列についている名前の配列
        /// </summary>
        public string[] DataNames;
        /// <summary>
        /// 名前をkeyとしてvalueをSeriesとしたHash
        /// </summary>
        private Hashtable NameToSeries = new Hashtable();
        /// <summary>
        /// Seriesの最大値
        /// </summary>
        public double Max = 0;
        /// <summary>
        /// Seriesの最小値
        /// </summary>
        public double Min = 0;

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="dataList"></param>
        public PlotViewModel(DataList dataList)
        {
            this.MyDataList = dataList;
            this.DataModel = new PlotModel();
            this.PrimitiveModel = new PlotModel();
            this.DataNames = MyDataList.DataNames.ToArray();

            foreach (var dataname in this.DataNames)
            {
                CreateLineSeries(dataname, 1, MyDataList.GetDataList(dataname));
                CreateLineSeries(dataname+"_P", 1, MyDataList.GetDataList(dataname));
            }
            foreach (var joint in JointNames.Joints)
            {
                CreateLineSeriesToPrimitive(joint+"_Primitive", 1, MyDataList.GetPrimitiveList(joint+"_Primitive"));
            }
            CreateLineSeries("ImagePosition", 1, new List<double[]>() { new double[] { 0, 0 }, new double[] { 0, 0 }, });
            CreateLineSeries("Threthold", 1, new List<double[]>() { new double[] { 0, 0 }, new double[] { 0, 0 }, });
            CreateLineSeriesToPrimitive("ImagePosition_P", 1, new List<double[]>() { new double[] { 0, 0 }, new double[] { 0, 0 }, });
        }

        /// <summary>
        /// DataNameとDataListからSeriesのを作成しDataNameと紐付ける
        /// </summary>
        /// <param name="dataName"></param>
        /// <param name="tickness"></param>
        /// <param name="dataList"></param>
        public void CreateLineSeries(String dataName, int tickness, List<double[]> dataList)
        {
            IList<DataPoint> points = new List<DataPoint>();
            foreach (var list in dataList) points.Add(new DataPoint(list[0], list[1]));
            LineSeries lineSeries = new LineSeries
            {
                Title = dataName,
                StrokeThickness = tickness,
                ItemsSource = points,
            };
            NameToSeries.Add(dataName, lineSeries);
        }

        /// <summary>
        /// DataNameとDataListからSeriesのを作成しDataNameと紐付ける
        /// </summary>
        /// <param name="dataName"></param>
        /// <param name="tickness"></param>
        /// <param name="dataList"></param>
        public void CreateLineSeriesToPrimitive(String dataName, int tickness, List<double[]> dataList)
        {
            IList<DataPoint> points = new List<DataPoint>();
            foreach (var list in dataList) points.Add(new DataPoint(list[0], list[1]));
            LineSeries lineSeries = new LineSeries
            {
                Title = dataName,
                StrokeThickness = tickness,
                ItemsSource = points,
            };
            NameToSeries.Add(dataName, lineSeries);
        }

        /// <summary>
        /// 描画するSeriesを追加
        /// </summary>
        /// <param name="dataName"></param>
        /// <param name="dataList"></param>
        public void AddLineSeriesToDataModel(String dataName)
        {
            this.DataModel.Series.Add((LineSeries)NameToSeries[dataName]);
        }

        /// <summary>
        /// 描画するSeriesを追加
        /// </summary>
        /// <param name="dataName"></param>
        /// <param name="dataList"></param>
        public void AddLineSeriesToPrimitiveModel(String dataName)
        {
            this.PrimitiveModel.Series.Add((LineSeries)NameToSeries[dataName]);
        }

        /// <summary>
        /// PositionSeriesのX軸の位置を変更
        /// </summary>
        /// <param name="xValue"></param>
        public void ChangePositionX(double xValue)
        {
            if (Max == Min) return;
            ((LineSeries)NameToSeries["ImagePosition"]).ItemsSource = new List<DataPoint>
            {
                new DataPoint(xValue, Max),
                new DataPoint(xValue, Min),
            };
        }

        public void ChangePositionXPrimitive(string joint, double xValue)
        {
            ((LineSeries)NameToSeries["ImagePosition_P"]).ItemsSource = new List<DataPoint>
            {
                new DataPoint(xValue, MyDataList.GetMax(joint + "_Speed")),
                new DataPoint(xValue, MyDataList.GetMin(joint + "_Speed")),
            };
        }

        public void ChangeThrethold(double yValue, double last)
        {
            ((LineSeries)NameToSeries["Threthold"]).ItemsSource = new List<DataPoint>
            {
                new DataPoint(0, yValue),
                new DataPoint(last, yValue),
            };
        }

        /// <summary>
        /// PositionSeriesのX軸の位置を変更
        /// </summary>
        /// <param name="xValue"></param>
        public void ChangePrimitive(string joint, double threthold)
        {
            List<DataPoint> dataPoints = new List<DataPoint>();
            foreach (var data in MyDataList.GetPrimitiveList(joint + "_Primitive"))
            {
                dataPoints.Add(new DataPoint(data[0], MyDataList.GetMin(joint + "_Speed")));
                if (0 < data[1] && data[1] < threthold)
                {
                    dataPoints.Add(new DataPoint(data[0], MyDataList.GetMax(joint + "_Speed")));
                    dataPoints.Add(new DataPoint(data[0], MyDataList.GetMin(joint + "_Speed")));
                }
            }
            ((LineSeries)NameToSeries[joint + "_Primitive"]).ItemsSource = dataPoints;
        }

        /// <summary>
        /// Seriesを削除
        /// </summary>
        public void RemoveSeriesFromDataModel(string dataName)
        {
            int index = this.DataModel.Series.IndexOf((LineSeries)NameToSeries[dataName]);
            this.DataModel.Series.RemoveAt(index);
        }

        public void RemoveSeriesFromPrimitiveModel(string dataName)
        {
            int index = this.PrimitiveModel.Series.IndexOf((LineSeries)NameToSeries[dataName]);
            this.PrimitiveModel.Series.RemoveAt(index);
        }

        /// <summary>
        /// すべてのSeriesをクリア
        /// </summary>
        public void ClearAllModel()
        {
            if (this.DataModel.Series.Count > 0) this.DataModel.Series.Clear();
            if (this.DataModel.Series.Count > 0) this.DataModel.Series.Clear();
        }

        /// <summary>
        /// すべてのSeriesをクリア
        /// </summary>
        public void ClearDataModel()
        {
            if (this.DataModel.Series.Count > 0) this.DataModel.Series.Clear();
        }

        /// <summary>
        /// すべてのSeriesをクリア
        /// </summary>
        public void ClearPrimitiveModel()
        {
            if (this.PrimitiveModel.Series.Count > 0) this.PrimitiveModel.Series.Clear();
        }

        /// <summary>
        /// 選択されているSeries中の最大値と最小値を求める
        /// </summary>
        public void SetSlectedSeries(List<string> selectedNames)
        {
            if (selectedNames.Count < 1)
            {
                Max = 0;
                Min = 0;
                return;
            }
            double max = MyDataList.GetMax(selectedNames[0]);
            double min = MyDataList.GetMin(selectedNames[0]);
            foreach (var name in selectedNames)
            {
                if (max < MyDataList.GetMax(name)) max = MyDataList.GetMax(name);
                if (min > MyDataList.GetMin(name)) min = MyDataList.GetMin(name);
            }
            Max = max;
            Min = min;
        }

        /// <summary>
        /// DataNameのSeriesが含まれているかを返す
        /// </summary>
        /// <param name="dataName"></param>
        /// <returns></returns>
        public bool ContainsInDataModel(string dataName)
        {
            return this.DataModel.Series.Contains((LineSeries)NameToSeries[dataName]);
        }

        /// <summary>
        /// DataNameのSeriesが含まれているかを返す
        /// </summary>
        /// <param name="dataName"></param>
        /// <returns></returns>
        public bool ContainsInPrimitiveModel(string dataName)
        {
            return this.PrimitiveModel.Series.Contains((LineSeries)NameToSeries[dataName]);
        }

        /// <summary>
        /// モデルの取得
        /// </summary>
        /// <returns></returns>
        public PlotModel GetDataModel()
        {
            return DataModel;
        }

        /// <summary>
        /// モデルの取得
        /// </summary>
        /// <returns></returns>
        public PlotModel GetPrimitiveModel()
        {
            return PrimitiveModel;
        }
    }
}