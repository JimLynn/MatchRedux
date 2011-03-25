﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Linq.Expressions;

namespace MatchRedux
{
    public class ViewModelBase : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        //protected void FireChanged(Expression<Func<object>> propertyExpression)
        //{
        //    PropertyChanged.Raise(propertyExpression);
        //}
        protected void FireChanged(string property)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(property));
            }
        }
    }

    //public static class PropertyChangedExtensions
    //{
    //    public static void Raise(this PropertyChangedEventHandler handler, Expression<Func<object>> propertyExpression)
    //    {
    //        if (handler != null)
    //        {
    //            // Retreive lambda body
    //            var body = propertyExpression.Body as MemberExpression;
    //            if (body == null)
    //                throw new ArgumentException("'propertyExpression' should be a member expression");

    //            // Extract the right part (after "=>")
    //            var vmExpression = body.Expression as ConstantExpression;
    //            if (vmExpression == null)
    //                throw new ArgumentException("'propertyExpression' body should be a constant expression");

    //            // Create a reference to the calling object to pass it as the sender
    //            LambdaExpression vmlambda = Expression.Lambda(vmExpression);
    //            Delegate vmFunc = vmlambda.Compile();
    //            object vm = vmFunc.DynamicInvoke();

    //            // Extract the name of the property to raise a change on
    //            string propertyName = body.Member.Name;
    //            var e = new PropertyChangedEventArgs(propertyName);
    //            handler(vm, e);
    //        }
    //    }
    //}
}
