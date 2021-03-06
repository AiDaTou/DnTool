﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DnTool.Utilities;
using DnTool.Models;
using System.Collections.ObjectModel;
using System.Diagnostics;
using Utilities.Dm;
using System.Windows.Threading;
using GalaSoft.MvvmLight.CommandWpf;
using Utilities.Tasks;


namespace DnTool.ViewModels
{
    public class TeleportViewModel:NotifyPropertyChanged
    {

        #region 命令
        public RelayCommand AddCurrentPointCommand { get; set; }
        public RelayCommand AddNewPointCommand { get; set; }
        public RelayCommand<Point> TeleportCommand { get; set; }
        public RelayCommand<Point> DeleteCommand { get; set; }
        public RelayCommand ModifyCommand { get; set; }
        public RelayCommand ImportListCommand { get; set; }
        public RelayCommand SaveListCommand { get; set; }
        public RelayCommand Redo { get; set; }
        public RelayCommand Undo { get; set; }
        public RelayCommand ClearCommand { get; set; }
        public RelayCommand SelectionChangedCommand { get; set; }
        #endregion

        public Point CurrentPoint { get; set; }
        public Point NewPoint { get; set; }
        public ObservableCollection<Point> Points { get; set; }
        
        public static DispatcherTimer timer = new DispatcherTimer();
        public ObservableCollection<File> Files { get; set; }
        private object _selectedValue;

        public object SelectedValue
        {
            get { return _selectedValue; }
            set 
            {
                _selectedValue = value;
                List<string> lines = FileOperateHelper.ReadFileLines(_selectedValue.ToString());
                this.Points.Clear();
                foreach (var line in lines)
                {
                    string[] temp = line.Split('#');
                    if (temp.Count() != 4)
                    {
                        Debug.WriteLine("路径:" + _selectedValue + ",格式不对");
                        Debug.WriteLine(line);
                        continue;
                    }
                    this.Points.Add(new Point(temp[0], float.Parse(temp[1]), float.Parse(temp[2]), float.Parse(temp[3])));
                }
            }
        }
        

       
        public TeleportViewModel()
        {
            this.CurrentPoint = new Point("当前坐标",0,0,0);
            this.NewPoint = new Point("添加坐标",0,0,0);
            this.Points = new ObservableCollection<Point>();
            this.Files = new ObservableCollection<File>();
            List<string> list = FileOperateHelper.GetFiles(AppDomain.CurrentDomain.BaseDirectory + "\\data", "*.txt");
            list.ForEach(x => Files.Add(new File() { Path = x, Name = System.IO.Path.GetFileNameWithoutExtension(x) }));

            this.AddCurrentPointCommand = new RelayCommand(() => this.AddPoint(CurrentPoint));
            this.AddNewPointCommand = new RelayCommand(() => this.AddPoint(NewPoint));
            this.ClearCommand = new RelayCommand(() => this.Clear());
            this.DeleteCommand = new RelayCommand<Point>((p)=>this.DeletePoint(p));
            this.TeleportCommand = new RelayCommand<Point>((p) =>this.Teleport(p));
            this.SaveListCommand = new RelayCommand(()=>this.SaveList());
         
            

            timer.Tick += (s, e) =>
            {
                if (SoftContext.Role == null)
                    return;
                DmPlugin dm = SoftContext.Role.Window.Dm;
                int hwnd = SoftContext.Role.Window.Hwnd;
                if (SoftContext.Role.Window.IsAlive)
                {
                    CurrentPoint.X = IntToFloat(dm.ReadInt(hwnd, "[1221740]+a5c", 0));
                    CurrentPoint.Y = IntToFloat(dm.ReadInt(hwnd, "[1221740]+a64", 0));
                    CurrentPoint.Z = IntToFloat(dm.ReadInt(hwnd, "[1221740]+a60", 0));
                }
                else
                {
                    CurrentPoint.X = 0;
                    CurrentPoint.Y = 0;
                    CurrentPoint.Z = 0;
                }

            };
            timer.Interval = TimeSpan.FromMilliseconds(500);
            timer.Start();
        }

       

        private void SaveList()
        {
            try
            {
                if (_selectedValue == null)
                    return;
                //if (FileOperateHelper.IsExists(_selectedValue.ToString()))
                //{
                //    var result=System.Windows.MessageBox.Show("文件已存在是否覆盖？","提示",System.Windows.MessageBoxButton.YesNo,System.Windows.MessageBoxImage.Question);
                //    if (result == System.Windows.MessageBoxResult.No)
                //        return;
                //}
                //Microsoft.Win32.SaveFileDialog dlg = new Microsoft.Win32.SaveFileDialog();
                //dlg.FileName = "User.txt"; // Default file name
                //dlg.DefaultExt = ".txt"; // Default file extension
                //dlg.Filter = "Text documents (.txt)|*.txt"; // Filter files by extension
                //dlg.InitialDirectory = AppDomain.CurrentDomain.BaseDirectory+"\\data";

                //// Show save file dialog box
                //Nullable<bool> result = dlg.ShowDialog();

                //// Process save file dialog box results
                //if (result == true)
                //{
                //    // Save document
                //    this.txtPlace.Text = dlg.FileName;
                //}

                string content = "";
                foreach (var p in Points)
                {
                    content += p.Name + "#" + p.X + "#" + p.Y + "#" + p.Z + "\r\n";
                }
                FileOperateHelper.WriteFile(_selectedValue.ToString(), content,true);
            }catch(Exception ex)
            {
                System.Windows.MessageBox.Show(ex.Message);
            }
        }
        private float IntToFloat(int val)
        {
            return float.Parse(BitConverter.ToSingle(BitConverter.GetBytes(val), 0).ToString("F1"));
        }
   
        private bool Teleport(Point point)
        {
            IRole role = SoftContext.Role;
            if (role == null)
                return false;
            DmPlugin dm = role.Window.Dm;
            int hwnd = role.Window.Hwnd;
            if (point == null)
            {
                Debug.WriteLine("坐标不能为null");
                return false;
            }
            else
            {
                if (role.Window.IsAlive)
                {

                    //if (point.X == null || point.Y == null || point.Z == null)
                    //{
                    //    Debug.WriteLine("坐标XYZ不能为null");
                    //    return false;
                    //}
                    Debug.WriteLine(point.ToString());
                    int a = dm.WriteFloat(hwnd, "[1221740]+a5c", point.X);
                    int b = dm.WriteFloat(hwnd, "[1221740]+a64", point.Y);
                    int c = dm.WriteFloat(hwnd, "[1221740]+a60", point.Z);
                    if (a == 1 && b == 1 && c == 1)
                    {
                        Debug.WriteLine("瞬移成功");
                        return true;
                    }
                    else
                    {
                        Debug.WriteLine("瞬移失败，写入X:{0}，Y:{1}，Z:{2}",a,b,c);
                        return false;
                    }
                }
                else
                {
                    Debug.WriteLine("窗口不存在:" + hwnd);
                    return false;
                }
            }
        }

        private void DeletePoint(Point p)
        {
            this.Points.Remove(p);
        }

        private void Clear()
        {
            this.Points.Clear();
        }

        private void AddPoint(Point point)
        {
            if (point == null)
                return;
            this.Points.Add(new Point(point.Name,point.X,point.Y,point.Z));
        }

      



      
        
        
    }
}
