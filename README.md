# NSMG
NSMG와 Hackfest를 위해서 만든 Repository 입니다. 

## 개발환경 구성

- Visual Studio 2017
- Slack
- Git
- Github

## 사전학습자료

- Event Husbs
- Stream Analysis Job
- Azure Functions
- Cosmos DB
- Azure Storage

## Artchitecture

## 기술 검증
![Artchitecture](https://github.com/KoreaEva/NSMG/blob/master/Images/Artchitecture.png?raw=true)<br>

### 1. EventHubs Client

 EventHub에 연결하기 위해서는 두 가지 방법 중 하나를 선택할 수 있다. 첫 번째 방법은 제공되는 패키지를 이용하는 방법이고 두 번째는 REST를 이용하는 연결 방법이다. 이번 Hackfest에서는 두 가지 방법을 모두 테스트 했다. 패키지를 이용하는 방법은 Nuget 을 사용하는 Console App으로 제작해서 테스트용 더미 클라이언트를 만드는데 사용했다. 

 EventHub Nuget Package [https://www.nuget.org/packages/Microsoft.Azure.EventHubs/](https://www.nuget.org/packages/Microsoft.Azure.EventHubs/)<br>
 EventHub REST [https://docs.microsoft.com/en-us/rest/api/eventhub/](https://docs.microsoft.com/en-us/rest/api/eventhub/)<br>

 더미 클라이언트의 전체 소스는 아래 링크에서 볼 수 있다.
 [https://github.com/KoreaEva/NSMG/tree/master/Samples/EventHubDataSender](https://github.com/KoreaEva/NSMG/tree/master/Samples/EventHubDataSender)<br>
 더미 클라이언트는 실제 업무에 사용되는 JSON 포멧을 발송하기 위해서 사용되었다. 

  REST를 사용하는 방법은 실제 Android 개발에 사용할 Library 제작에 사용 되었다. 

### 2. SAS Token From Azure Function
Azure 기반의 Mobile App 을 만들면, 처음엔 Connection String 을 사용해서 프로토타이핑을 하면 쉽고 빠르지만, 상용 서비스를 위한 앱을 만들 때에는 보안을 위해서 SAS Token 을 사용해야 한다. (Cloud Service Resource 들에 대한 접근 권한 부여를 위한 다른 방법도 있다.)
이 과정에 있어, Mobile App 에서 REST API 로 Azure PaaS Service 에 접근하기 위한 SAS Token 을 서버 또는 브로커로 부터 생성해서 받아와야하는데, 이를 Azure Function 으로 구현하면 대단히 편하다. 실제로 Azure Function 에 Azure Storage 용 SAS Token 생성 템플릿이 있기도 하다.
Visual Studio Community 를 사용해서 Function 의 코드를 편집할 경우 아래와 같이 하면 된다.
1. 솔루션탐색기에서 local.settings.json 을 열어서 아래와 같은 형태로 세 줄을 추가한다.
    "eventHubResourceUri": "https://myeventhub.servicebus.windows.net/",
    "eventHubKeyName": "RootManageSharedAccessKey",
    "eventHubKey": "uqC11pox5syZy9QF7jFGwDfJO4abCaCxYyuX5Khrr0U="
2. 위의 상수 값들은 로컬 테스트에서만 사용되며, Azure Function App 에 게시한 이후에는, Function App 의 Application Settings 에서 위와 동일한 이름으로 추가한 상수가 사용된다. 
3. eventHubResourceUri, eventHubKeyName, eventHubKey 의 값은 Azure Portal 에서 얻을 수 있다.
4. Function 함수의 내용이 담겨 있는 .cs 파일의 내용은 아래와 같다.
```csharp
using System;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;
namespace SASTokenApp
{
    public static class SASTokenForEventHubs
    {
        [FunctionName("SASTokenForEventHubs")]
        public static async Task<HttpResponseMessage> Run([HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)]HttpRequestMessage req, TraceWriter log)
        {
            log.Info("C# HTTP trigger function processed a request.");
            // 로컬 또는 Azure Function App 의 Application Settings 에서 가져오게될 환경변수 값.
            string resourceUri = System.Environment.GetEnvironmentVariable("eventHubResourceUri", EnvironmentVariableTarget.Process);
            string keyName = System.Environment.GetEnvironmentVariable("eventHubKeyName", EnvironmentVariableTarget.Process);
            string key = System.Environment.GetEnvironmentVariable("eventHubKey", EnvironmentVariableTarget.Process);
            
            // Token 의 유효기간을 설정하기 위한 코드.
            TimeSpan sinceEpoch = DateTime.UtcNow - new DateTime(1970, 1, 1);
            var week = 60 * 60 * 24 * 365;
            var expiry = Convert.ToString((int)sinceEpoch.TotalSeconds + week);
            
            // 인코딩을 거쳐서 SAS Token 생성.
            string stringToSign = HttpUtility.UrlEncode(resourceUri) + "\n" + expiry;
            HMACSHA256 hmac = new HMACSHA256(Encoding.UTF8.GetBytes(key));
            var signature = Convert.ToBase64String(hmac.ComputeHash(Encoding.UTF8.GetBytes(stringToSign)));
            var sasToken = String.Format(CultureInfo.InvariantCulture, "SharedAccessSignature sr={0}&sig={1}&se={2}&skn={3}", HttpUtility.UrlEncode(resourceUri), HttpUtility.UrlEncode(signature), expiry, keyName);
            return req.CreateResponse(HttpStatusCode.OK, sasToken);
        }
    }
}
```
Azure Function 은 마이크로 서비스의 대표적인 형태이며, 실행시간 100ms 기준으로 리퀘스트 1백만번에 200원 정도 밖에 안되니 걱정 없이 써도 좋다. Token 발급만을 위해서 VM 을 두는 것은 좀 낭비일 것이다.^^
먼저, Function 을 통해서 토큰을 받으면 아래와 같은 형태일 것이다.
"SharedAccessSignature sr=https%3a%2f%2fmshub.servicebus.windows.net%2f&sig=PFZVab43PMsO0q9gz4%2bFsuaQq%5ff05L4M7hKVBN8DEn0%3d&se=1553339810&skn=RootManageSharedAccessKey"
만약 Android App 에서 REST API 로 Function 을 호출해서 위의 Token 을 얻었다면 맨 앞의 " 문자와 맨 끝의 " 문자를 제거해주어야 한다. 제거 없이 바로 EventHub 에 데이터를 Post 하는 데 사용하시면 아래와 같은 오류가 발생할 것이다.
MalformedToken: The credentials contained in the authorization header are not in the WRAP format.
Android App 에서 Token 을 받아온 후, EventHub 로 데이터를 전송하는 코드는 아래와 같다.
build.gradle 파일의 dependencies 에 아래를 추가해주면 okHttp 를 사용할 수 있다.
~~~
implementation 'com.squareup.okhttp3:okhttp:3.10.0'
~~~
Azure Function 및 EventHub 에 REST 로 접속하기 위한 HTTP Client 는 여러 사용자들의 이해를 돕기 위해 각각 다른 것을 사용해보았다.
MainActivity.java
~~~java
public class MainActivity extends AppCompatActivity {
    TextView textViewA;
    HttpURLConnection conn = null;
    final String TAG = "MainActivity";
    String authorizationString;
    @Override
    protected void onCreate(Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);
        setContentView(R.layout.activity_main);
        textViewA = (TextView)findViewById(R.id.textView);
    }
    public void testFunctionWithPOST(View v) {
        Thread threadA = new Thread(new Runnable() {
            @Override
            public void run() {
                try {
                    String query = URLEncoder.encode("mU4kdaut8IfDR5UTRA2NMCjtpMxrhKNCIv6oLeMhhW384a021HYkOw==", "utf-8");
                    URL url = new URL("https://myfunc.azurewebsites.net/api/SASTokenForEventHubs?code="+query);
                    conn = (HttpURLConnection)url.openConnection();
                    if (conn != null) {
                        conn.setConnectTimeout(10000);
                        conn.setReadTimeout(10000);
                        conn.setRequestMethod("POST");
                        conn.setUseCaches(false);
                        conn.setDoInput(true);      // InputStream From Server
                        conn.setDoOutput(true);     // OutputStream with POST
                        // Request Header값 셋팅
                        conn.setRequestProperty("Content-Type", "application/json");    // 서버로 Request Body 전달시 json 일 때.
                        //conn.setRequestProperty("Accept", "application/json");          // 서버 Response Data를 json 으로 요청.
                        // 실제 서버로 Request 요청 하여 응답 수신.
                        final int responseCode = conn.getResponseCode();
                        if(responseCode == HttpURLConnection.HTTP_OK || responseCode == HttpURLConnection.HTTP_CREATED || responseCode == HttpURLConnection.HTTP_ACCEPTED) {
                            Log.d(TAG,"Connection OK");
                            BufferedReader br = new BufferedReader(new InputStreamReader(conn.getInputStream(), "UTF-8"));
                            StringBuilder sb = new StringBuilder();
                            String line = null;
                            while ((line = br.readLine()) != null) {
                                if(sb.length() > 0) {
                                    sb.append("\n");
                                }
                                sb.append(line);
                            }
                            final String resultA = sb.toString();
                            authorizationString = resultA.replaceAll("\"","");
                            Log.d("Res ", authorizationString);
                            runOnUiThread(new Runnable() {
                                @Override
                                public void run() {
                                    textViewA.setText(authorizationString);
                                }
                            });
                        } else {
                            Log.d(TAG,"Connection Error :" + responseCode);
                            runOnUiThread(new Runnable() {
                                @Override
                                public void run() {
                                    textViewA.setText("Connection Error :" + responseCode);
                                }
                            });
                        }
                    }
                } catch(Exception ex) {
                    Log.d(TAG, "Exception: "+ex);
                    ex.printStackTrace();
                }  finally {
                    if(conn != null) {
                        conn.disconnect();
                    }
                }
            }
        });
        threadA.start();
    }
    public void sendDataToEventHubs(View v) {
        final MediaType JSON = MediaType.parse("application/json; charset=utf-8");
        Thread threadA = new Thread(new Runnable() {
            @Override
            public void run() {
                try {
                    OkHttpClient.Builder builderA = new OkHttpClient.Builder().protocols(Arrays.asList(Protocol.HTTP_1_1));
                    OkHttpClient clientA = builderA.build();
                    HttpUrl.Builder urlBuilder = HttpUrl.parse("https://myeventhub.servicebus.windows.net/myhub/mes").newBuilder();
                    String urlA = urlBuilder.build().toString();
                    String jsonA = "{\"name\":\"mingyu\", \"age\":\"20\"}";
                    RequestBody body = RequestBody.create(JSON, jsonA);
                    Request request = new Request.Builder()
                            .header("Authorization", authorizationString)
                            .header("Content-Type", "application/json")
                            .header("ContentType","application/atom+xml;type=entry;charset=utf-8")
                            .url(urlA)
                            .post(body)
                            .build();
                    Response response = clientA.newCall(request).execute();
                    //Log.d("Code",""+response.code());
                    //Log.d("Body", response.body().string());
                    response.close();
                } catch (IOException e) {
                    e.printStackTrace();
                }
            }
        });
        threadA.start();
    }
}
~~~
activity_main.xml
~~~xml
<?xml version="1.0" encoding="utf-8"?>
<RelativeLayout xmlns:android="http://schemas.android.com/apk/res/android"
    xmlns:app="http://schemas.android.com/apk/res-auto"
    xmlns:tools="http://schemas.android.com/tools"
    android:layout_width="match_parent"
    android:layout_height="match_parent"
    tools:context=".MainActivity">
    <Button
        android:text="Get SAS Token"
        android:layout_width="wrap_content"
        android:layout_height="wrap_content"
        android:layout_centerVertical="true"
        android:layout_centerHorizontal="true"
        android:onClick="testFunctionWithPOST"
        android:id="@+id/button" />
    <Button
        android:text="Send Data to Event Hubs"
        android:layout_width="wrap_content"
        android:layout_height="wrap_content"
        android:layout_below="@id/button"
        android:layout_centerHorizontal="true"
        android:onClick="sendDataToEventHubs"
        android:id="@+id/buttonB" />
    <TextView
        android:text="Function Test"
        android:layout_width="wrap_content"
        android:layout_height="wrap_content"
        android:layout_marginBottom="87dp"
        android:id="@+id/textView"
        android:textSize="30sp"
        android:layout_above="@+id/button"
        android:layout_centerHorizontal="true" />
</RelativeLayout>
~~~

### 3. EventHub

1초에 5000개 이상의 요청을 안정적으로 처리하기 하기 위해서 IoT Hub와 EventHub 그리고 10개의 Azure Storage Queue에 분산처리 하는 방법 등 다양한 방법이 고려되었다. 이 중에서도 초기에 EventHub는 모바일 디바이스쪽으로 Callback할 수 있는 방법이 제공되지 않아서 제외되었고 IoT Hub의 경우 비용에 대한 문제로 제외되었다. Queue를 사용하는 방식으로 거의 결졍되었다가 디바이스 방향으로의 Callback을 Google이 제공하는 push notification으로 처리하게 되면서 Event Hub를 사용하기로 결정되었다. 

EventHub는 Standard 버전의 경우 1개의 인스턴스가 초당 1000개의 메시지를 처리 할 수 있다. 따라서 10개를 사용하면 무난하게 요구사항을 맞출 수 있을 것으로 기대 된다. 

![EventHub](https://github.com/KoreaEva/NSMG/blob/master/Images/EventHubs.png?raw=true)<br>

EventHub를 사용할 때 EventHub의 처리량은 충분한데 Partition에서 받아들이지 못할 수 있다. 하지만 [공식웹페이지](https://docs.microsoft.com/en-us/azure/event-hubs/event-hubs-quotas)에서 제공되는 정보만으로는 1개의 Partition의 처리량을 확인 할 수 있기 때문에 일반적인 Blob Storage의 처리량을 기준으로 1개의 Partition 당 1초에 2000개 정도의 요구 사항을 처리한다고 가정해서 Partition은 4개로 설정했다. 

 2000 * 4개 = 8000 정도의 성능이 나올 것으로 기대 된다. 

### 4. Stream Analytics Job

EventHub 에서 제공하는 Capture기능을 사용하면 EventHub로 들어오는 데이터를 Blob storage에 백업할 수 있다. 

하지만 실제로 테스트 해보니 한글이 깨지고 Line단위로 분할하는 등의 기능이 미해서 결국 중간에 Stream Analytics Job을 사용해서 저장하기로 했다.
또 필요에 따라서 추가적인 데이터의 흐름이 필요할 수도 있어서 Stream Analytics를 사용하게 되었다. 


### 3. Azure Functions (EventHub Trigger)

EventHub를 사용하게 되면서 Azure Functions도 역시 EventHub Trigger를 사용하게 되었다. 

```csharp
    public static class EventHubWifi
    {
        [FunctionName("EventHubWifi")]
        public static void Run([EventHubTrigger("wifi", Connection = "WIFI")]string myEventHubMessage, TraceWriter log)
        {
            log.Info($"C# Event Hub trigger function processed a message: {myEventHubMessage}");
        }
    }
```
Azure Functions를 사용할 때에 주의 할 점은 실제 가동중인 Azure Functions와 Local에서 개발중인 Azure Functions의 storage를 같은 서비스로 지정하게 되면 Connection limit가 초과 되었다는 메시지와 함께 실행되지 않는 경우가 있다. 

### 4. MySQL to SQL data migration 

### 5. SQL Utility library

### 6. Cosmos DB

Cosmos DB에 저장하기 위해서는 Database를 먼저 생성하고 JSON Document를 저장할 수 있는 Collection을 생성해야 한다. 실제 저장은 Collection에 이루어지는데 Collection도 10Gbyte 한계가 있는 버전과 Unlimited 버전이 존재한다. Unlimited을 사용할 때에는 사실상 RU의 제약이 없이 사용할 수 있으며 또 RU의 제한을 넘어서 사용하는 것도 별도의 요청을 통해서 가능하게 되어 있다. 반대로 10Gbyte의 한계가 있는 버전의 경우에는 10,000 RU가 한계다. 그래서 NSMG의 요구사항을 수용하기 위해서는 Unlimited Collection을 사용하게 되었다. 

```csharp
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Azure.WebJobs.ServiceBus;

using System;
using System.Net;
using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace NSMGFunctions
{
    public static class EventHubWifi
    {
        private const string EndpointUrl = "<EndPoint URL>";
        private const string PrimaryKey = "<Primary Key>";
        private static DocumentClient client = null;
        private static string DatabaseName = "<Database Name>";
        private static string DataCollectionName = "<Collection Name>";

        [FunctionName("EventHubWifi")]
        public async static void Run([EventHubTrigger("wifi", Connection = "WIFI")]string myEventHubMessage, TraceWriter log)
        {
            try
            {
                client = new DocumentClient(new Uri(EndpointUrl), PrimaryKey);

                // Azure Function의 효율을 위해서 Database와 Collection을 체크하고 생성하는 코드는 모두 주석처리 했다. 

                //await client.CreateDatabaseIfNotExistsAsync(new Database { Id = DatabaseName });
                //await client.CreateDocumentCollectionIfNotExistsAsync(
                //    UriFactory.CreateDatabaseUri(DatabaseName), 
                //    new DocumentCollection { Id = DataCollectionName },
                //    new RequestOptions { OfferThroughput = 5000}
                //);

                string documentID = generateID();
                JObject json = JObject.Parse(myEventHubMessage);

                log.Info($"C# Event Hub trigger function processed a message: {myEventHubMessage}");
                await client.CreateDocumentAsync(UriFactory.CreateDocumentCollectionUri(DatabaseName, DataCollectionName), json);
                
            }
            catch(DocumentClientException de)
            {
                Exception baseException = de.GetBaseException();
                string errorMessage = string.Format("error occurred : {0}. Message: {1},", de.StatusCode, de.Message);
                log.Info(errorMessage);
            }
            catch(Exception e)
            {
                string errorMessage = string.Format("Message: {0},", e.Message);
                log.Info(errorMessage);
            }
            finally
            {

            }
        }

        /// 문서의 고유이름 생성하기 위한 메소드
        public static string generateID()
        {
            return string.Format("{0}_{1:N}", System.DateTime.Now.Ticks, Guid.NewGuid());
        }
    }
}
```

Cosmos DB에 필요한 RU를 계산해 볼 수 있는 사이트 [https://www.documentdb.com/capacityplanner#](https://www.documentdb.com/capacityplanner#)<br

Cosmos DB개발 참조링크
[https://docs.microsoft.com/ko-kr/azure/cosmos-db/sql-api-get-started](https://docs.microsoft.com/ko-kr/azure/cosmos-db/sql-api-get-started)<br>

### 7. Azure Batch

### 8. Azure Search

### 9. Azure Functions(Http Trigger)


## 프로젝트 코드

## Collaboration Tool

[NSMG Hackfest Slack](http://nsmg-hackfest.slack.com)<br>
[One Drive](https://1drv.ms/f/s!AosfFsO-w03gjnOhsZl1McXhzLP4)


## Azure Services

[Azure Web App](https://docs.microsoft.com/ko-kr/azure/app-service/app-service-web-overview)<br>
[IoT Hub](https://docs.microsoft.com/ko-kr/azure/iot-hub/)<br>
[Stream Analytics Job](https://docs.microsoft.com/ko-kr/azure/stream-analytics/)<br>
[Azure Storage Account](https://docs.microsoft.com/ko-kr/azure/storage/common/storage-introduction)<br>
[SQL Database](https://docs.microsoft.com/ko-kr/azure/sql-database/sql-database-technical-overview)<br>
[Azure Functions](https://docs.microsoft.com/ko-kr/azure/azure-functions/functions-overview)<br>
[Cosmos DB](https://docs.microsoft.com/ko-kr/azure/cosmos-db/introduction)

## 관련 문서 


## 사용을 예상했으나 사용되지 않은 서비스 



### 1. IoT Hub Dummy --> Android Client

IoT Hub Dummy를 사용해서 대량의 데이터를 전송하는 것을 시도했으나 충분한 트래픽을 발생시키는 데에는 1개의 클라이언트에서 한계가 있었고 또 네트웍등의 문제가 발생하는 것을 확인했다. 
 또 IoT Hub가 비용적인 문제와 좀 더 유연한 확장을 위해서 Azure Storage Account에서 제공하는 Queue를 사용하게 되면서 아래의 Iot Hub Dummy는 사용하지 않게 되었다. 대신 Android Client를 만들어서 테스트하게 되었으며 Android Client는 Queue에 직접 통신하게 되었다. 

 #### Android Client
 <내용 추가>

 #### IoT Hub Dummy (사용하지 않음)

테스트르 위해서 충분한 숫자의 데이터 패킷을 발생시키기 위한 더미 소스 입니다.<br> 
[IoTHub Sensor Data Dummy C# Source Code](https://github.com/KoreaEva/NSMG/tree/master/Samples/IoTHubDataSender)<br>

사용하기 위해서는 실행 파일에서 아래와 같이 실행하게 되면 5초단위로 1개의 인스턴스에서 패킷을 발송하게 됩니다. <br>
IoTHubDataSender 5000 1<br>

충분한 숫자의 패킷을 발송하기 위해서는 IoTHubDataSender 1 5000 이렇게 실행시키면 1000/1 초 단위로 5000천개의 인스턴스에서 패킷을 발송하게 됩니다. <br>
지금은 온도, 습도, 미세먼지 값등을 더미로 발생시켜서 보내고 있다. 실제 데이터로 바꾸기 위해서는 아래 두 개의 파일을 수정해야 한다. <br>

WeatherModel.cs //데이터 모델<br>
DummySensor.cs  //난수 발생기 <br>

### 2. Azure Storage Queue

Azure Storage Queue와 관련된 자세한 내용은 [https://azure.microsoft.com/ko-kr/services/storage/queues/](https://azure.microsoft.com/ko-kr/services/storage/queues/
)에서 확인 할 수 있다. 

![Artchitecture](https://github.com/KoreaEva/NSMG/blob/master/Images/Queues.png?raw=true)<br>

여기서는 초당 5000개 이상의 요청을 처리하기 위해서 10개의 Queue를 사용 하고 있다. 그리고 각각의 Queue에는 Queue Trigger 담당하는 Azure Functions들을 연결해서 들어오는 요청을 처리 할 수 있게 했다. 

Queue의 제약은 아래와 같다. 
Azure Storage Account 초당 20,000 호출<br>
Azure Storage Queue 초당 2,000 호출<br>

### 2. IoT Hub Trigger

![IoT Hub Setting](https://github.com/KoreaEva/NSMG/blob/master/Images/IoTHubTriggerSetting.png?raw=true)
<br>

IoT Hub Trigger를 사용하기 위해서는 EndPoint에 지정된 값들을 소스에 반영해서 코딩해야 한다. 

```csharp
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Azure.WebJobs.ServiceBus;

namespace IoTHubTrigger
{
    public static class IoTHubTrigger
    {
        [FunctionName("IoTHubTrigger")]
        public static void Run([EventHubTrigger("nsmgiothub", Connection = "IoTHubConnection")]string myEventHubMessage, TraceWriter log)
        {
            log.Info($"C# Event Hub trigger function processed a message: {myEventHubMessage}");
        }
    }
}
```

### 3. Blob Trigger
Blob Trigger의 경우 Blob안에 저장되어 있는 파일의 Stream이 리턴된다. 따라서 업데이트 되는 부분만 추출하기 위해서는 마지막 라인만 가져오는 방법이 필요하다. 
 어떻게 해도 파일 단위의 작업이기 때문에 불가피하게 오버해드가 발생 된다. 그래서 Batch 작업등에서만 사용하는 것이 적절한 방법으로 판단된다. 
개별적인 업데이트 내용을 핸들링 하기 위해서는 IoTHub Trigger를 사용하는 것이 적합해 보인다. 

```csharp
    public static class BlobTriggerJson
    {
        [FunctionName("BlobTriggerJson")]
        public static void Run([BlobTrigger("tempdata/{name}", Connection = "BlobStorage")]Stream myBlob, string name, TraceWriter log)
        {
            StreamReader sr = new StreamReader(myBlob);

            string temp = sr.ReadLine(); //이렇게 하면 첫째 줄만 계속 읽게 되면 그래서 sr.ReadToEnd()를 사용해야 하는데 그럼 사이즈가 커지면 심각한 오버해드가 발생할 예정.

            JObject json = JObject.Parse(temp);

            log.Info($"DeviceID: {json.Last["DeviceID"].ToString()} Temperature: {json.Last["Temperature"].ToString()} Humidity: {json.Last["Humidity"].ToString()}");
            
            //log.Info($"C# Blob trigger function Processed blob\n Name:{name} \n Size: {myBlob.Length} Bytes");
        }
    }
```

### 4. Timer Trigger

Timer setting 방법 [https://gs.saro.me/#!m=elec&jn=866](https://gs.saro.me/#!m=elec&jn=866)<br>

```csharp
    public static class TimerTrigger
    {
        [FunctionName("TimerTrigger")]
        public static void Run([TimerTrigger("0/10 * * * * *")]TimerInfo myTimer, TraceWriter log)
        {
            log.Info($"C# Timer trigger function executed at: {DateTime.Now}");
        }
    }
```

### 6. Log Analytics

일반적인 용도로 사용할 수 없어서 제외되었다.