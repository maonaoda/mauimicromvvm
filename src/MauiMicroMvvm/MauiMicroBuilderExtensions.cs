using MauiMicroMvvm;
using MauiMicroMvvm.Internals;
using INavigation = MauiMicroMvvm.INavigation;

namespace Microsoft.Maui.Hosting;

public static class MauiMicroBuilderExtensions
{
    public static MauiAppBuilder UseMauiMicroMvvm<TShell>(this MauiAppBuilder builder)
        where TShell : Shell
    {
        builder.Services
            .AddSingleton<TShell>()
            .AddSingleton<IWindowCreator, WindowCreator<TShell>>()
            .AddSingleton<IViewFactory, ViewFactory>()
            .AddSingleton<INavigation, DefaultNavigation>()
            .AddSingleton<IPageDialogs, PageDialogs>()
            .AddScoped<ViewModelContext>();
        return builder;
    }

    [Obsolete("Use `UseMauiMicroMvvm<TShell>()` instead.")]
    public static MauiAppBuilder UseMauiMicroMvvm<TApp, TShell>(this MauiAppBuilder builder, params string[] mergedDictionaries)
        where TApp : Application
        where TShell : Shell =>
        builder.UseMauiMicroMvvm<TApp, TShell>([], mergedDictionaries);

    [Obsolete("Use `UseMauiMicroMvvm<TShell>()` instead.")]
    public static MauiAppBuilder UseMauiMicroMvvm<TApp, TShell>(this MauiAppBuilder builder, ResourceDictionary resources, params string[] mergedDictionaries)
        where TApp : Application
        where TShell : Shell
    {
        builder.UseMauiApp<TApp>();

        builder.Services
            .AddSingleton<TApp>()
            .AddSingleton<IApplication>(sp =>
            {
                var app = sp.GetRequiredService<TApp>();

                if (mergedDictionaries.Length != 0)
                {
                    var assembly = typeof(TShell).Assembly;
                    var qualifiedResources = mergedDictionaries.Select(x =>
                    {
                        if (x.Contains(";assembly="))
                            return $@"    <ResourceDictionary Source=""{x}"" />";
                        return $@"    <ResourceDictionary Source=""{x};assembly={assembly.GetName().Name}"" />";
                    });

                    var xaml = $@"
<ResourceDictionary
    xmlns=""http://schemas.microsoft.com/dotnet/2021/maui""
    xmlns:x=""http://schemas.microsoft.com/winfx/2009/xaml"">
  <ResourceDictionary.MergedDictionaries>
{string.Join('\n', qualifiedResources)}
  </ResourceDictionary.MergedDictionaries>
</ResourceDictionary>";
                    
                    app.Resources.LoadFromXaml(xaml);
                }

                if (resources != null && resources.Keys.Count != 0)
                {
                    app.Resources.Add(resources);
                }

                return app;
            });

        return builder
            .UseMauiMicroMvvm<TShell>();
    }

    [Obsolete("Use `UseMauiMicroMvvm<TShell>()` instead.")]
    public static MauiAppBuilder UseMauiMicroMvvm<TShell>(this MauiAppBuilder builder, params string[] mergedDictionaries)
        where TShell : Shell =>
        builder.UseMauiMicroMvvm<TShell>([], mergedDictionaries);

    [Obsolete("Use `UseMauiMicroMvvm<TShell>()` instead.")]
    public static MauiAppBuilder UseMauiMicroMvvm<TShell>(this MauiAppBuilder builder, ResourceDictionary resources, params string[] mergedDictionaries)
        where TShell : Shell =>
        builder.UseMauiMicroMvvm<Application, TShell>(resources, mergedDictionaries);

    public static IServiceCollection MapView<TView, TViewModel>(this IServiceCollection services, string? key = null)
        where TView : VisualElement
        where TViewModel : class
    {
        if (string.IsNullOrEmpty(key))
            key = typeof(TView).Name;

        if (typeof(TView).IsAssignableTo(typeof(Page)))
            Routing.RegisterRoute(key, typeof(TView));

        return services.AddTransient<TView>(sp =>
        {
            var viewFactory = sp.GetRequiredService<IViewFactory>();
            var view = viewFactory.CreateView<TView>();
            viewFactory.Configure(view);
            return view;
        })
            .AddSingleton(new ViewMapping(key, typeof(TView), typeof(TViewModel)))
            .AddTransient<TViewModel>();
    }
}