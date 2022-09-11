using k8s;
using k8s.Models;
using System.Linq;
using System.Security;
using System.Xml.Linq;

namespace DotnetKubeOperatorTest;

public abstract class CustomResource : KubernetesObject
{
    public V1ObjectMeta Metadata { get; set; }
}
public abstract class CustomResourceList<TCustomResource> : KubernetesObject
    where TCustomResource : CustomResource
{
    public V1ListMeta Metadata { get; set; }

    public List<TCustomResource> Items { get; set; }
}

public abstract class CustomResource<TSpec, TStatus> : CustomResource
{
    public TSpec Spec { get; set; }
    public TStatus Status { get; set; }
}
public class MyThigSpec
{
    public string MyProperty { get; set; }
}

public class MyThingResourceList
    : CustomResourceList<MyThingResource>
{ }

public class MyThingResource
    : CustomResource<MyThigSpec, Object>
{ }

internal class Program
{
    private static async Task Main(string[] args)
    {
        var config = KubernetesClientConfiguration.BuildConfigFromConfigFile();

        IKubernetes client = new Kubernetes(config);

        var x = 0;

        var list = await client.CoreV1.ListNamespacedConfigMapWithHttpMessagesAsync("default", fieldSelector: "metadata.name=my-config");

        var configMap = list.Body.Items.FirstOrDefault();

        if (configMap == null)
        {
            configMap = new V1ConfigMap()
            {
                Metadata = new V1ObjectMeta
                {
                    Name = "my-config"
                },
                Data = new Dictionary<string, string>
                {
                    ["key1"] = "this"
                }
            };

            await client.CoreV1.CreateNamespacedConfigMapWithHttpMessagesAsync(configMap, "default");
        }

        var obj = client.CustomObjects.ListClusterCustomObjectWithHttpMessagesAsync("panditha.com",
            "v1",
            "mythings",
            //allowWatchBookmarks: true,
            watch: true);

        var dict = new Dictionary<string, string>();

        await foreach (var (type, myThing) in obj.WatchAsync<MyThingResource, object>())
        {
            x++;
            Console.WriteLine($"==on watch event {x} ==");
            Console.WriteLine(type);
            switch (type)
            {
                case WatchEventType.Added:
                    dict.Add($"{myThing.Metadata.Namespace()}-{myThing.Metadata.Name}", myThing.Spec.MyProperty);
                    break;
                case WatchEventType.Modified:
                    dict[$"{myThing.Metadata.Namespace()}-{myThing.Metadata.Name}"] =  myThing.Spec.MyProperty;
                    break;
                case WatchEventType.Deleted:
                    dict.Remove($"{myThing.Metadata.Namespace()}-{myThing.Metadata.Name}");
                    break;
                case WatchEventType.Error:
                    break;
                case WatchEventType.Bookmark:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            Console.WriteLine(myThing.Metadata.Name);
            Console.WriteLine($"property = {myThing.Spec.MyProperty}");
            Console.WriteLine($"==on watch event {x} ==");

            var xml = "<r>" +
                      string.Concat(dict.Select(kv =>
                          $"<e><k>{SecurityElement.Escape(kv.Key)}</k><k>{SecurityElement.Escape(kv.Value)}</k></e>")) +
                      "</r>";
            

            // configMap.Data = new Dictionary<string, string>
            // {
            //     ["XML"] = xml
            // };
            
            await client.CoreV1.ReplaceNamespacedConfigMapWithHttpMessagesAsync(
                body: new V1ConfigMap
                {
                    Metadata = new V1ObjectMeta
                    {
                        Name = "my-config"
                    },
                    Data = new Dictionary<string, string>
                    {
                        ["XML"] = xml
                    }
                }, 
                name: @"my-config",  
                namespaceParameter: "default");

            await Task.Delay(5);

        }
        // C# 8 required https://docs.microsoft.com/en-us/archive/msdn-magazine/2019/november/csharp-iterating-with-async-enumerables-in-csharp-8


        // await foreach (var k in res.Wat(CancellationToken.None))
        // {
        //     
        // }
        //
        // //await foreach (var w in podlistResp.W)
        // {
        //     Console.WriteLine($"==on watch event {x} ==");
        //     Console.WriteLine(type);
        //     Console.WriteLine(item.Metadata.Name);
        //     var jsonString = JsonSerializer.Serialize(item);
        //     Console.WriteLine($"==on watch event {x++} ==");
        // }

        //var podlistResp = client.CoreV1.ListNamespacedPodWithHttpMessagesAsync("default", watch: true);
        //await foreach(var yy in podlistResp.WatchAsync<V1p>())
        // // // C# 8 required https://docs.microsoft.com/en-us/archive/msdn-magazine/2019/november/csharp-iterating-with-async-enumerables-in-csharp-8
        // await foreach (var (type, item) in podlistResp.WatchAsync<V1Pod, V1PodList>())
        // {
        //     Console.WriteLine($"==on watch event {x} ==");
        //     Console.WriteLine(type);
        //     Console.WriteLine(item.Metadata.Name);
        //     var jsonString = JsonSerializer.Serialize(item);
        //     Console.WriteLine($"==on watch event {x++} ==");
        // }

        // uncomment if you prefer callback api
        // WatchUsingCallback(client);
    }

    private static void WatchUsingCallback(IKubernetes client)
    {
        var podlistResp = client.CoreV1.ListNamespacedPodWithHttpMessagesAsync("default", watch: true);
        using (podlistResp.Watch<V1Pod, V1PodList>((type, item) =>
               {
                   Console.WriteLine("==on watch event==");
                   Console.WriteLine(type);
                   Console.WriteLine(item.Metadata.Name);
                   Console.WriteLine("==on watch event==");
               }))
        {
            Console.WriteLine("press ctrl + c to stop watching");

            var ctrlc = new ManualResetEventSlim(false);
            Console.CancelKeyPress += (sender, eventArgs) => ctrlc.Set();
            ctrlc.Wait();
        }
    }
}

internal class Town
{
}

internal class Funky
{
}