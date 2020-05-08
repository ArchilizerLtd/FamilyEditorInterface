using System;
using System.Windows;
using System.Windows.Markup;

namespace Dialog.Service
{
    public class DataTemplateManager
    {
        /// <summary>
        /// Creates a DataTemplate in Code
        /// https://www.codeproject.com/Articles/444371/Creating-WPF-Data-Templates-in-Code-The-Right-Way
        /// </summary>
        /// <param name="viewModelType">The viewmodel type</param>
        /// <param name="viewType">The view type</param>
        /// <returns></returns>
        private static DataTemplate CreateTemplate(Type viewModelType, Type viewType)
        {
            const string xamlTemplate = "<DataTemplate DataType=\"{{x:Type vm:{0}}}\"><v:{1} /></DataTemplate>";
            var xaml = String.Format(xamlTemplate, viewModelType.Name, viewType.Name, viewModelType.Namespace, viewType.Namespace);

            var context = new ParserContext();

            context.XamlTypeMapper = new XamlTypeMapper(new string[0]);
            context.XamlTypeMapper.AddMappingProcessingInstruction("vm", viewModelType.Namespace, viewModelType.Assembly.FullName);
            context.XamlTypeMapper.AddMappingProcessingInstruction("v", viewType.Namespace, viewType.Assembly.FullName);

            context.XmlnsDictionary.Add("", "http://schemas.microsoft.com/winfx/2006/xaml/presentation");
            context.XmlnsDictionary.Add("x", "http://schemas.microsoft.com/winfx/2006/xaml");
            context.XmlnsDictionary.Add("vm", "vm");
            context.XmlnsDictionary.Add("v", "v");

            var template = (DataTemplate)XamlReader.Parse(xaml, context);
            return template;
        }
        /// <summary>
        /// Register a DataTemplate establishing the connection between the Viewmodel and the View
        /// </summary>
        /// <typeparam name="T1"></typeparam>
        /// <typeparam name="T2"></typeparam>
        internal void RegisterDataTemplate<T1, T2>()
        {
            var template = DataTemplateManager.CreateTemplate(typeof(T1), typeof(T2));  //Create a DataTemplate
            var key = template.DataTemplateKey; //Create a key for the DataTempalte
            var app = Application.Current;
            if(app == null)
            {
                app = new System.Windows.Application();
            }
            app.Resources.Add(key, template);   //Registering the DataTempalte with the Application
        }
    }
}
