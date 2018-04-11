NSMG 작업을 하면서 몇 가지 주요 주제를 가지고 아키텍쳐를 설계하고 변화하는 과정을 거쳤다. 
NSMG의 요구사항은 다음과 같이 정리 할 수 있다. 

1. 지금 사용하고 있는 AWS 보다 비용이 충분히 감소할 수 있어야 한다. 
2. 1초에 5000개 이상의 요청을 실시간으로 처리 할 수 있어야 한다. 
3. 관리 및 운영을 쉬워야 한다. 
4. 편의상 개발 언어를 C#으로 변경 할 수도 있지만 학습 및 시행착오를 최소화 할 수 있어야 한다. 

이와 같은 요구사항을 바탕으로 아래와 같이 다양한 버전의 아키텍쳐를 검토했다. 

## 1. IoTHub를 이용한 구성
![Artchitecture](https://github.com/KoreaEva/NSMG/blob/master/Images/Artchitecture/1.PNG?raw=true)<br>

IoT Hub를 사용한 구성을 처음에 구성했을 때에는 클라이언트의 안드로이드 디바이스에 메시지를 Push하기 위해서 IoTHub를 사용하는 것으로 구성했다. 최종 결과물은 CosmosDB에 저장하는 것으로 지정했다. 이 구성이 AWS에서 제공하는 기능에 비해서 가장 큰 장점은 IoT Hub에 동시 접속 가능한 디바이스의 수에 제한이 없다는 것이다. 하지만 초당 5000건을 처리하기 위해서는 IoT Hub 중에서도 상대적으로 비싼 사이즈의 서비스를 사용해야 했고 또 초당 5000건의 데이터를 Azure Functions의 Timer Trigger를 사용한다면 Azure Fucntions의 가동 시간이 길어져 사용 비용의 증가가 예상되는 등의 문제가 있었다. 

## 2. IoT Hub를 제외하고 EventHub의 사용
![Artchitecture](https://github.com/KoreaEva/NSMG/blob/master/Images/Artchitecture/2.PNG?raw=true)<br>

 IoTHub를 사용해야 했던 Push기능을 Google의 Push Notification 서비스를 사용하게 되면서 IoTHub를 꼭 사용할 필요가 없어졌다. 따라서 사용자의 요청을 모두 Http Trigger를 사용하는 Azure Functions를 사용하는 것으로 변경했다. 그리고 쏟아지는 데이터를 EventHub에서 받아서 넘기는 구조를 사용했다. EventHub의 경우 1개의 인스턴스가 1000개의 연결을 지원하기 때문에 10개의 인스턴스를 생성한다면 최대 10000개의 요청을 처리할 수 있는 용량이 확보되기 때문에 사용이 고려 되었다. 

## 3. EventHub를 제외하고 Queue를 여러개 사용
![Artchitecture](https://github.com/KoreaEva/NSMG/blob/master/Images/Artchitecture/3.PNG?raw=true)<br>

Queue를 10개 사용하게 되다면 가격과 성능을 모두 잡을 수 있다는 아이디어가 반영된 아키텍쳐이다. Azure Storage에서 제공하고 있는 Queue는 1초에 2000개까지 요청을 받을 수 있기 때문에 안드로이드에서 Random * 10으로 10개의 Queue에 무작위로 쌓게 한다. Queue에 데이터가 들어올 때 마다 Queue Trigger를 사용해서 SQL Server에 있는 데이터를 매핑하는 작업을 한다. 
 특이한 것은 Azure에서 제공하는 Log analytics를 사용해서 데이터를 쌓는 것을 고려했다. Log analytics는 그 자체로 분석 기능이 있고 1개월이 지난 데이터들는 상대적으로 저렴한 비용으로 저장할 수 있다. 

![Artchitecture](https://github.com/KoreaEva/NSMG/blob/master/Images/Artchitecture/4.PNG?raw=true)<br>

## 4. EventHub 다시 사용하는 구성
![Artchitecture](https://github.com/KoreaEva/NSMG/blob/master/Images/Artchitecture/5.PNG?raw=true)<br>

Queue를 10개 사용할 경우 관리 포인트가 늘어난다는 단점이 부각되어서 다시 EventHub가 부각되었다. 이번에는 안드로이드에서 바로 EventHub를 호출하게 하므로써 Azure Function을 일일히 호출하지 않아서 비용의 감소가 이루어졌다. 또 Log analytics가 배제 되었는데 그 이유는 Log analytics는 VM이나 웹서버등의 log를 관리하기 위한 용도로 제공되는 서비스라 이번 작업에는 적절하지 못했기 때문이다. 

## 5. EventHub를 두 개 사용하는 구성
![Artchitecture](https://github.com/KoreaEva/NSMG/blob/master/Images/Artchitecture/6.PNG?raw=true)<br>

첫 번째 EventHub는 안드로이드 폰에서 전달되는 데이터를 받고 데이터가 들어오는데로 Azure Functions에서  EventHub Trigger를 이용해서 데이터를 메핑헤서 데이터 백업용으로 Blob storage에 저장하고 실제로 활용하는 데이터는 CosmosDB에 저장한다. 그리고 데이터를 분석하기 위해서 Azure Search를 사용을 고려했다. 

## 6. Azure Batch의 추가
![Artchitecture](https://github.com/KoreaEva/NSMG/blob/master/Images/Artchitecture/7.PNG?raw=true)<br>

Azure Batch를 추가했다. CosmosDB에 쌓여 있는 정보를 Blob Storage에 저장하고 다시 SQL Server에 반영하는 부분을 부하가 적은 새벽에 돌리기 위해서 Azuzre Batch를 사용하는 것을 고려했다. 하지만 확인 결과 Azure Batch는 해당 용도 보다는 NPC 시나리오에 적합했기 때문에 다른 방법을 고려하게 되었다. 

## 7. Azure Functions과 CosmosDB사이에 Blob Storage를 사용
![Artchitecture](https://github.com/KoreaEva/NSMG/blob/master/Images/Artchitecture/8.PNG?raw=true)<br>

구성을 쉽게 할 수 있는 장점이 있었으나 모든 부하가 Storage로 집중되면서 트레픽을 소화하지 못하는 현상이 발견되었다. 또 다른 부분은 원본 데이터를 저장하기 위해서 EventHub에서 제공하는 Capture 기능을 이용해서 Blob storage에 저장하는 것을 테스트 했지만 JSON이 Line분할이 안돼고 한글이 깨지는 등의 문제가 발생했다. 

## 8. 최종 아키텍쳐
![Artchitecture](https://github.com/KoreaEva/NSMG/blob/master/Images/Artchitecture/9.PNG?raw=true)<br>

여러단계를 거쳐서 최종적으로 완성된 아키텍쳐이다. 보안을 위하서 SAS Token를 가져와서 적용하는 Azure Functions이 추가되었다. 원본데이터는 EventHub에서 Stream Analytics Job을 거쳐서 Blob Storage에 저장한다. EventHub에 들어온 데이터들은 EventHub Trigger를 이용하는 Azure Functions를 통해서 SQL Database와 데이터 매핑을 한 뒤 CosmosDB에 저장한다. 
 CosmosDB에 저장된 데이터는 가상머신에 걸려 있는 Batch작업을 통해서 새벽에 Blob Storage에 백업되고 또 SQL Database에 변경된 내용이 업데이트 된다. 
Timer Trigger를 사용하는 Azure Functions를 통해서 하루 한번씩 CosmosDB에서 3개월 이상된 데이터를 모두 삭제해서 용량을 최소한으로 관리한다. 

![Artchitecture](https://github.com/KoreaEva/NSMG/blob/master/Images/Artchitecture/10.PNG?raw=true)<br>
