﻿using System.ComponentModel;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;
using PropertyChangingEventArgs = System.ComponentModel.PropertyChangingEventArgs;
using PropertyChangingEventHandler = System.ComponentModel.PropertyChangingEventHandler;

namespace MauiMicroMvvm;

public abstract class MauiMicroViewModel : INotifyPropertyChanging, INotifyPropertyChanged, IViewModelActivation, IViewLifecycle, IAppLifecycle, IQueryAttributable, IDisposable
{
    private readonly Dictionary<string, object> _properties = [];
    private readonly Lazy<ILogger> _lazyLogger;
    private readonly object _locker = new ();

    protected MauiMicroViewModel(ViewModelContext context)
    {
        Navigation = context.Navigation;
        PageDialogs = context.PageDialogs;
        _lazyLogger = new Lazy<ILogger>(() => context.Logger.CreateLogger(GetType().Name));
        QueryParameters = new Dictionary<string, object>();
    }

    protected bool IsDisposed { get; private set; }

    protected ILogger Logger => _lazyLogger.Value;

    protected INavigation Navigation { get; }

    protected IPageDialogs PageDialogs { get; }

    public event PropertyChangedEventHandler? PropertyChanged;
    public event PropertyChangingEventHandler? PropertyChanging;

    public bool IsBusy
    {
        get => Get<bool>();
        set => Set(value, () => Set(!value, nameof(IsNotBusy)));
    }

    public bool IsNotBusy => Get<bool>();

    protected IDictionary<string, object> QueryParameters { get; private set; }

    public virtual void OnFirstLoad() { }

    public virtual void OnAppearing() { }

    public virtual void OnDisappearing() { }

    public virtual void OnResume() { }

    public virtual void OnSleep() { }

    protected virtual void OnParametersSet() { }

    protected T Get<T>(T? defaultValue = default, [CallerMemberName]string? propertyName = null)
    {
        ArgumentException.ThrowIfNullOrEmpty(propertyName);
        if (_properties.TryGetValue(propertyName, out var value) && value is T valueAsT)
            return valueAsT;

        if (defaultValue is null && typeof(T).IsValueType)
            defaultValue = Activator.CreateInstance<T>();

        if (Nullable.GetUnderlyingType(typeof(T)) != null)
            ArgumentNullException.ThrowIfNull(defaultValue);

        return defaultValue!;
    }

    protected bool Set<T>(T value, [CallerMemberName]string? propertyName = null)
    {
        ArgumentException.ThrowIfNullOrEmpty(propertyName);

        var isSet = _properties.ContainsKey(propertyName);
        if (isSet && EqualityComparer<T>.Default.Equals(Get<T>(propertyName: propertyName), value))
            return false;

        RaisePropertyChanging(propertyName);

        if (value is null && isSet)
        {
            _properties.Remove(propertyName);
        }
        else if (value is not null)
        {
            _properties[propertyName] = value;
        }

        RaisePropertyChanged(propertyName);
        return true;
    }

    protected virtual void RaisePropertyChanging(string propertyName)
    {
        PropertyChanging?.Invoke(this, new PropertyChangingEventArgs(propertyName));
    }

    protected virtual void RaisePropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    protected bool Set<T>(T value, Action callback, [CallerMemberName]string? propertyName = null)
    {
        if (Set(value, propertyName))
        {
            callback();
            return true;
        }

        return false;
    }

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        QueryParameters = query ?? new Dictionary<string, object>();
        if (query is null || !query.Any())
            return;

        var properties = GetType().GetProperties();
        foreach((var key, var value) in query)
        {
            var propInfo = properties.FirstOrDefault(p => p.Name.Equals(key, StringComparison.InvariantCultureIgnoreCase));
            if (propInfo is not null)
            {
                PropertyChanging?.Invoke(this, new PropertyChangingEventArgs(propInfo.Name));
                _properties[propInfo.Name] = Convert.ChangeType(value, propInfo.PropertyType);
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propInfo.Name));
            }
        }

        OnParametersSet();
    }

    /// <summary>
    /// Performs application-defined tasks associated with freeing, releasing, or resetting
    /// unmanaged resources.
    /// </summary>
    protected virtual void Dispose() { }

    void IDisposable.Dispose()
    {
        lock(_locker)
        {
            if (!IsDisposed)
            {
                Dispose();
            }
            IsDisposed = true;
        }

        GC.SuppressFinalize(this);
    }
}
