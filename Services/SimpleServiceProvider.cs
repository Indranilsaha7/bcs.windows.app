using System;
using System.Collections.Generic;
using System.Windows;

namespace BarodaPOS.Services
{
    public class SimpleServiceProvider : IServiceProvider
    {
        private readonly Dictionary<Type, object> _pages = new();

        public object GetService(Type serviceType)
        {
            if (serviceType == null) return null;

            if (typeof(FrameworkElement).IsAssignableFrom(serviceType))
            {
                if (!_pages.TryGetValue(serviceType, out var page))
                {
                    page = Activator.CreateInstance(serviceType);
                    if (page != null)
                    {
                        _pages.Add(serviceType, page);
                    }
                }
                return page;
            }
            
            return null;
        }
    }
}
