﻿using DnTool.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Utilities.Dm;
using Utilities.Log;
using Utilities.Tasks;
namespace DnTool.GameTask
{
    public class BuyThingsTask:TaskBase
    {
        /// <summary>
        /// 任务设置，可用属性为：.Thing .Num .UseLB
        /// </summary>
        private MallThing _thing;
        private int _num;
        private bool _useLB;
        public BuyThingsTask(TaskContext context)
            : base(context)
        {
            Logger.Debug("1");
           _thing = context.Settings.Thing;
           _num = context.Settings.Num;
           _useLB = context.Settings.UseLB;
           Logger.Debug("2");
        }
        protected override void StepsInitialize(ICollection<TaskStep> steps)
        {
            steps.Add(new TaskStep { StepName = "购买商城物品", Order = 1, RunFunc = RunStep1 });
        }


        private TaskResult RunStep1(TaskContext context)
        {
            IRole role = context.Role;
            Role r = (Role)role;
            DmPlugin dm = role.Window.Dm;
            int hwnd=role.Window.Hwnd;
           
            if (_useLB)
            {
                if (!_thing.CanUseLB)
                    return new TaskResult(TaskResultType.Failure, "无法使用龙币购买物品“{0}”.".FormatWith(_thing.Name));
                if (r.MallLB < _thing.Value)
                    return new TaskResult(TaskResultType.Failure, "龙币不足,无法购买物品“{0}”.".FormatWith(_thing.Name));
            }
            else
            {
                if(r.MallVolume<_thing.Value)
                    return new TaskResult(TaskResultType.Failure, "点卷不足,无法购买物品“{0}”.".FormatWith(_thing.Name));
            }

            bool ret=Delegater.WaitTrue(() =>
            {
                dm.MoveToClick(567,43);
                return dm.SendString(hwnd,_thing.Name)==1?true:false;
            }, () => dm.Delay(1000), 10);
            if(ret==false) 
                return new TaskResult(TaskResultType.Failure,"发送文本数据失败,无法购买物品“{0}”.".FormatWith(_thing.Name));

            ret = Delegater.WaitTrue(() =>
            {
                dm.MoveToClick(766, 43);
                return role.HasMallThing(_thing.Name) ? true : false;
            }, () => 
            {
                dm.Delay(1000);
                dm.MoveToClick(567, 43);
                dm.SendString(hwnd, _thing.Name);                
            }, 5);
            if (ret == false) return new TaskResult(TaskResultType.Failure, "无法找到该商品“{0}”.".FormatWith(_thing.Name));

            if (role.FindMallButtonAndClick(_thing.Name))
            {
                
               Delegater.WaitTrue(() => role.HasDialogButton("取消") ,()=>dm.Delay(1000));
               if (_useLB)
                   dm.MoveToClick(939, 574);
               role.FindDialogButtonAndClick("购买");
               Delegater.WaitTrue(() => role.HasDialogButton("是"), () => dm.Delay(1000));
               role.FindDialogButtonAndClick("是");
               Delegater.WaitTrue(() => role.HasDialogButton("确认"), () => dm.Delay(1000));
               role.FindDialogButtonAndClick("确认");
            }
            _num--;
            return _num<=0? TaskResult.Success : RunStep1(context); 
        }

        protected override int GetStepIndex(TaskContext context)
        {
            IRole role = context.Role;
           
           //有是空腹 返回1
            //快捷键 无 返回2
            //防御》=20 返回3
            //返回4
            return 1;
        }
    }
}
