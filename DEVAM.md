# KAN BAĞI — Proje Devir Dosyası

> Bu dosya, yeni bir sohbet oturumunda projeye kaldığı yerden devam edebilmek için hazırlandı.
> **Kodların tamamı diskte mevcut** — okumak için: `D:\KAHVE DENEME\KAHVEHANEDENEME\Assets\_Project\Scripts\`

---

## 1. ROL VE GÖREV

Sen kıdemli bir Unity (C#) oyun geliştiricisi ve sistem mimarısın. "Kan Bağı" adında, 3D URP tabanlı, hikaye ve işletme odaklı bir kahvehane/mahalle simülasyonu geliştiriyoruz.

## 2. OYUNUN RUHU

Karakterimiz hapisten yeni çıkmış ve amcasının bıraktığı izbe bir çay ocağını işleterek sıfırdan yükselmeye çalışıyor. Sadece bir dükkan değil, yaşayan küçük bir mahallemiz var. Mekanı işleterek, bilgisayardan stok ve dekorasyon yönetimi yaparak (Supermarket Simulator tarzı) dükkanı büyüteceğiz. Para ve "İtibar" (Reputation) kazandıkça mahalledeki diğer dükkanları satın alabilecek, en sonunda dükkanın arkasına gizli kumarhane açarak mafya/polis sarmalına gireceğiz.

## 3. YAZILIM ANAYASASI (KESİN KURALLAR)

1. **SOLID ve Temiz Kod** — Spagetti kod kesinlikle yasak. Sınıflar sadece kendi işini yapar.
2. **Observer Pattern** — Sistemler birbirine `Action` / `UnityEvent` ile bağlanır. `Update` içinde başka script'i sürekli kontrol etmek (polling) yasak.
3. **Veri Yönetimi** — Ürünler, fiyatlar, envanter verileri kesinlikle **ScriptableObject** (`ItemData`) ile tutulur.
4. **UI Yönetimi** — Arayüz için **TextMeshPro**. UI kodları oyun mantığından (Manager'lar vb.) tamamen ayrı tutulur.

## 4. ÇALIŞMA AKIŞI (kullanıcının açık talebi)

1. Her adımda ne yapılacağı **önce anlatılır, onay alınır**.
2. Onaydan sonra kod yazılır.
3. Unity Editor içinde yapılması gerekenler (sahne kurulumu, Inspector bağlantıları, NavMesh bake vb.) **adım adım kullanıcıya tarif edilir** — kullanıcı kendisi yapar.
4. Kullanıcı "yaptım / çalışıyor" deyince sonraki adıma geçilir.

**İzinsiz kod yazılmaz.** Adımlar küçük tutulur (kullanıcı büyük adımları ikiye bölmeyi tercih ediyor).

---

## 5. PROJE ORTAMI

| | |
|---|---|
| Proje yolu | `D:\KAHVE DENEME\KAHVEHANEDENEME` |
| Unity | `6000.3.10f1` (Unity 6.3) |
| Render Pipeline | URP 17.3.0 |
| Input | **New Input System 1.18.0** — `Assets/InputSystem_Actions.inputactions`, project-wide actions olarak atanmış |
| Navigation | AI Navigation 2.0.10 (NavMeshSurface ile bake edildi) |
| UI | uGUI 2.0.0 + TextMeshPro |
| Aktif sahne | `Assets/Scenes/SampleScene.unity` (dukkan sahnesi; henuz yeniden adlandirilmadi) |

### Klasör yapısı
```
Assets/_Project/
├── Prefabs/           (CayBardagi, Musteri)
├── ScriptableObjects/ (Cay)
├── Scenes/            (Dukkan)
├── Materials/
└── Scripts/
    ├── Core/          ReputationManager, UpgradeManager, UpgradeUnlocker, DayCycleManager,
                       DailyStatsManager, ISaveable, SaveManager, PropertyManager
    ├── Customers/     Customer, CustomerSpawner, CustomerModifiers, CustomerPatienceBoost
    ├── Data/          ItemData, ItemDatabase, ShopCatalog, UpgradeData, UpgradeCatalog,
                       PropertyData, PropertyCatalog
    ├── Delivery/      DeliveryManager
    ├── Editor/        RowPrefabBuilder, ReportPanelBuilder, SaveMenuItems, SignPrefabBuilder,
                       GamblingSignBuilder, MafiaPrefabBuilder, PolicePrefabBuilder
    ├── Illegal/       GamblingTable, SuspicionManager, MafiaManager, MafiaCollector,
                       PoliceManager, PoliceOfficer
    ├── Neighborhood/  PropertyBuilding, PropertyVisualSwitch
    ├── Economy/       EconomyManager, PurchaseManager
    ├── Interaction/   IInteractable, HoldableItem, PlayerInteractor
    ├── Inventory/     InventoryManager
    ├── Items/         TeaGlass, DeliveryBox
    ├── Player/        PlayerController, PlayerHands, PlayerLock
    ├── Shop/          TimedStation, TeaStove, Sink, GlassShelf, Table, TableManager,
                       ComputerStation, StorageRack, MenuManager, StationSpeedBoost
    └── UI/            MoneyUI, ReputationUI, StockUI, HeldItemUI, InteractionPromptUI, ProgressBarUI,
                       OrderBubbleUI, Billboard, ComputerUI, CatalogEntryUI, DeliveryNotificationUI,
                       TabGroupUI, UpgradeListUI, UpgradeEntryUI, ClockUI, DailyReportUI,
                       PropertyListUI, PropertyEntryUI, PropertySignUI, GamblingSignUI, MafiaSignUI,
                       PoliceNotificationUI
```

### Kod konvansiyonları (mevcut koda uyulmalı)
- Namespace'ler: `KanBagi.Core`, `KanBagi.Customers`, `KanBagi.Data`, `KanBagi.Economy`, `KanBagi.Interaction`, `KanBagi.Inventory`, `KanBagi.Items`, `KanBagi.Player`, `KanBagi.Shop`, `KanBagi.UI`
- Yorumlar **Türkçe ama şapkasız/noktasız** (ASCII) — `cay`, `musteri`, `siparis` gibi. Kod içinde Türkçe karakter kullanılmıyor.
- Manager'lar `public static X Instance { get; private set; }` singleton deseni kullanıyor (`EconomyManager`, `ReputationManager`, `PlayerHands`, `TableManager`).
- Inspector alanları mümkün olduğunca **opsiyonel**: boşsa kod otomatik bulur (`GetComponentInChildren`, `FindFirstObjectByType`, ofset fallback'leri). Bulamazsa **Türkçe, adresi net `Debug.LogError`** basar ve component'i `enabled = false` yapar.
- Durum makineleri `Update` yerine **coroutine** ile yazılıyor.

---

## 6. ŞU ANA KADAR YAPILANLAR

### Adım 1 — Çekirdek mimari ✅
- `ItemData` (ScriptableObject): itemName, description, icon, itemType (`Beverage`/`Consumable`/`TableItem`), sellPrice, purchaseCost, prepareDuration. Menü: `Create > Kan Bagi > Item Data`
- `EconomyManager`: `CurrentMoney`, `AddMoney(int)`, `TrySpendMoney(int) : bool`, `event Action<int> OnMoneyChanged`. Başlangıç 250 TL.
- `ReputationManager`: `CurrentReputation`, `AddReputation(int)`, `event Action<int> OnReputationChanged`. Başlangıç 0. **Faz 4'te mafya/polis buna abone olacak.**
- `MoneyUI`: sadece `OnMoneyChanged` dinler.

### Adım 2 — Oyuncu ve etkileşim ✅
- `PlayerController`: FPS yürüme + bakış (CharacterController + Input System "Player" map'i, `Move`/`Look`). Kamerayı ve input asset'ini otomatik bulur.
- `IInteractable`: `string InteractionPrompt { get; }` + `void Interact()`
- `PlayerInteractor`: kameradan raycast (2.5m), odak değişince `event Action<IInteractable> OnFocusChanged`, `Interact` action'ının `started`'ında `Interact()` çağırır. Etkileşim sonrası ipucunu tazeler; nesne yok edildiyse odağı null'lar. Oyuncunun kendi collider'ını yok sayar. **Tek raycast + `hit.collider.TryGetComponent`** kullanır → `IInteractable` script'i collider ile **aynı GameObject'te** olmalı.
- `InteractionPromptUI`: Canvas üzerinde, `[E] ...` ipucu.
- ~~`MoneyTestInteractable`~~: geçici test kutusuydu, **Adım 4b'de silindi**.

### Adım 3a — Taşıma sistemi ve bardak ✅
- `HoldableItem` (`IInteractable`): `DisplayName`, `IsHeld`, `AttachTo(Transform)`, `Detach(pos, rot, restorePhysics)`, `SetPickupEnabled(bool)`. Ele alınınca collider/rigidbody kapanır.
- `PlayerHands` (singleton): tek slot. `HeldItem`, `IsEmpty`, `TryHold(HoldableItem) : bool`, `ReleaseHeldItem() : HoldableItem`, `TryDropAt(...)`, `event Action<HoldableItem> OnHeldItemChanged`. Tutma noktasını (`HoldAnchor`) kameranın altında **otomatik oluşturur**.
- `TeaGlass`: `enum GlassState { Empty, Full, Dirty }`, `State`, `Content (ItemData)`, `TryFill(ItemData) : bool`, `MarkDirty()`, `Clean()`, `GetStateLabel()`, `event Action<GlassState> OnStateChanged`. Görselleri (`liquidVisual`, `dirtyVisual`) duruma göre açıp kapatır.
- `GlassShelf` (`IInteractable`): temiz bardak stoğunun **tek sahibi**. `CleanGlassCount`, `AddCleanGlass()`, `event Action<int> OnCleanGlassCountChanged`. Bardak verir / boş bardağı geri alır.
- `HeldItemUI`: elde ne olduğunu gösterir, bardağın durum değişimlerine de abone olur.

### Adım 3b — Çay ocağı ve lavabo ✅ (bulaşık döngüsü kapandı)
- `TimedStation` (abstract, `IInteractable`): süreli işlerin ortak temeli. `IsBusy`, `WorkLabel`, `StartWork(float, Action)`, `event Action<TimedStation, float> OnProgressChanged`, `event Action<TimedStation> OnWorkFinished`.
- `TeaStove : TimedStation`: elde boş bardak varsa alır, `beverageData.PrepareDuration` kadar doldurur, sonra "Bardağı Al". Doldururken bardak alınamaz.
- `Sink : TimedStation`: kirli **veya dolu** ("Dök ve Yıka") bardağı alır, yıkar, bardağı yok edip `GlassShelf.AddCleanGlass()` ile **stoğa iade eder**. Rafı otomatik bulur.
- `ProgressBarUI`: istasyonları otomatik bulur, ilerleme çubuğunu doldurur.

### Adım 4a — Masa ve müşteri ✅
- `Table`: `Occupant`, `IsFree`, `SeatPosition`, `GlassPosition`, `GlassRotation`, `TryReserve(Customer)`, `Release()`. Ofsetler fallback'li, Scene'de gizmo çizer (mavi = koltuk, sarı = bardak).
- `TableManager` (singleton, `[DefaultExecutionOrder(-100)]`): `Register/Unregister`, `TryReserveFreeTable(Customer, out Table)`, `HasFreeTable()`.
- `Customer` (NavMeshAgent + coroutine durum makinesi): `enum CustomerState { Arriving, WalkingToTable, WaitingForOrder, Leaving }`, `State`, `IsWaitingForOrder`, `Initialize(Vector3 exit)`, event'ler: `OnStateChanged`, `OnPatienceChanged(float)`, `OnLeft(Customer)`. Sabır 25 sn; masaya oturunca agent kapanır, kalkınca `NavMesh.SamplePosition` + `Warp` ile geri açılır.
- `CustomerSpawner`: aralıklı üretim, `maxActiveCustomers` limiti, boş masa yoksa göndermez.
- `OrderBubbleUI` + `Billboard`: müşterinin başında "Çay" balonu; sabır azaldıkça yazı beyazdan kırmızıya döner.

### Adım 4b — Servis ve ekonomi ✅ (FAZ 1 KAPANDI)
- `CustomerState`'e **`Drinking`** eklendi: `Arriving → WalkingToTable → WaitingForOrder → Drinking → Leaving`.
- `Customer` yeni API'si:
  - `ItemData Order` (Inspector: `desiredOrder` — prefab'da `Cay` atalı)
  - `bool CanAcceptOrder(TeaGlass)` — sipariş bekliyor + bardak `Full` + içerik siparişle uyuşuyor. `desiredOrder` boşsa herhangi bir `Beverage` kabul edilir. **Masanın ipucu metni ve teslim anı bu tek kuralı paylaşır.**
  - `bool TryServe(TeaGlass)` — kabul ederse satışı işler ve `Drinking`'e geçer.
  - Inspector: `drinkDuration` (6 sn), `reputationOnServed` (+1), `reputationOnAngryLeave` (1 → eksi olarak işlenir).
- `WaitForOrderRoutine` artık `State == WaitingForOrder` koşuluyla döner → servis edilince **sabır döngüsünden kendiliğinden çıkar** (polling yok).
- `CompleteSale(ItemData)`: `EconomyManager.AddMoney(SellPrice)` + `ReputationManager.AddReputation(+1)`. **Para/itibar mantığının tek yeri burası** — ileride `SalesManager`'a taşınacaksa değişecek tek metot.
- `LosePatience()`: servis edilmeden gidince `AddReputation(-1)`.
- `DrinkRoutine`: içerken bardağın `HoldableItem.SetPickupEnabled(false)` (oyuncu çalamaz), süre bitince `MarkDirty()` + pickup tekrar açılır → **kirli bardak masada kalır**.
- `Table` artık `IInteractable`:
  - Bardağı `GlassPosition/GlassRotation`'a `Detach(restorePhysics: false)` ile bırakır (kinematik kalır, yuvarlanmaz), ardından `SetPickupEnabled(true)` ile collider'ı geri açar.
  - `bool HasGlass` — `placedGlassItem != null && !placedGlassItem.IsHeld`; oyuncu bardağı alınca veya lavaboda yok edilince yuva **kendiliğinden** boşalır.
  - İpuçları: "Siparisi Ver" / "Musteri bunu istemiyor" / "Once masadaki bardagi kaldir" / "Musteri oturuyor" / "Bos masa".
  - Bilinçli kural: **müşteri yokken masaya çay bırakılamaz.**
- Kirli bardağı almak için Table'a gerek yok — bardağın kendi `HoldableItem`'ı zaten `IInteractable`.
- `OrderBubbleUI`: sipariş adı artık `customer.Order.ItemName`'den okunur (ItemData yoksa `orderMessage` yedeği).
- `ReputationUI`: `MoneyUI`'ın kardeşi, sadece `OnReputationChanged` dinler.

### Adım 5a — Stok çekirdeği ✅ (Faz 2 başladı)
- `InventoryManager` (singleton, `KanBagi.Inventory`): sarf malzemesi stoğunun **tek sahibi**.
  - `int GetCount(ItemData)`, `bool HasStock(ItemData, int = 1)`, `bool TryConsume(ItemData, int = 1)`, `void Add(ItemData, int = 1)`
  - `event Action<ItemData, int> OnStockChanged` — ileride yönetim PC'si de buna abone olacak.
  - Başlangıç stoğu Inspector'dan: `List<StockEntry>` (nested `[Serializable]` sınıf, `Item` + `Amount`). Aynı malzeme iki kez yazılırsa toplanır.
  - `Dictionary<ItemData, int>` — key olarak ScriptableObject referansı kullanılıyor.
- Yeni ScriptableObject: **`CayYapragi`** (Consumable, SellPrice 0, PurchaseCost 2).
- `TeaStove` güncellemesi:
  - Yeni alanlar `requiredConsumable` + `consumeAmount`. **Boş bırakılırsa ocak eskisi gibi bedava çalışır** (geriye dönük güvenli).
  - Malzeme **doldurma başlamadan** düşülür (`TryConsumeIngredient`) — bardak ocakta unutulsa bile yaprak yandı sayılır.
  - Stok yoksa ipucu `"Cay Yapragi bitti"` olur, `Interact()` hiçbir şey yapmaz, **bardak oyuncunun elinde kalır**.
  - Stok uyarısı el boşken de görünür (oyuncu boşuna bardak taşımasın).
  - `Start()`'ta InventoryManager yoksa Türkçe LogError (Awake'te bakılmıyor — manager'ın Awake'i sonra çalışmış olabilir).
- `StockUI`: sadece `OnStockChanged` dinler, `trackedItems` listesindeki malzemeleri `StringBuilder` ile satır satır yazar.

### Adım 5b — Yönetim PC'si ✅ (anında teslimat)
- `ShopCatalog` (ScriptableObject, `Create > Kan Bagi > Shop Catalog`): satın alınabilir malzeme listesi.
  - `CatalogEntry`: `Item` + `UnitsPerPack`; **`PackPrice => item.PurchaseCost * UnitsPerPack`** — fiyat koda gömülmez.
  - `IsValid` — eksik doldurulmuş satırlar UI'da gösterilmez.
  - Asset: `MalzemeKatalogu` (CayYapragi × 10 = 20 TL).
- `PurchaseManager` (singleton, `KanBagi.Economy`): **satın almanın tek yeri.**
  - `bool CanAfford(entry)` (buton aktifliği için), `bool TryBuy(entry)` → `TrySpendMoney` + `InventoryManager.Add`
  - `event Action<ItemData, int> OnPurchased`
  - **5c'de değişecek tek metot `TryBuy`** — malzeme depoya değil kuryeye gidecek.
- `PlayerLock` (singleton, Player objesinde): `SetControlEnabled(bool)` → `PlayerController` + `PlayerInteractor` aç/kapa, `event Action<bool> OnControlEnabledChanged`. `PlayerController` zaten `OnEnable/OnDisable`'da imleci yönettiği için **imleç bedavaya doğru çalışıyor.** Faz 4'te kumar masası/polis diyaloğu da bunu kullanacak.
- `ComputerStation` (`IInteractable`): ekranı açar/kapar, `event Action<bool> OnScreenToggled`. ESC için Input System'deki **`UI/Cancel`** action'ına ekran açıkken abone olur (asset'te yoksa sessizce atlar, Kapat butonu kalır).
- `ComputerUI`: **Canvas üzerinde durur** (panelin üzerinde değil — panel kapalıyken `Start()` çalışmaz!). Katalogdan satırları `entryPrefab`'tan kodla üretir; ekran açıkken para/stok event'lerine abone olur, kapanınca aboneliği bırakır.
- `CatalogEntryUI`: tek satır — ad, "10 adet - 20 TL (Depo: 30)", Satın Al butonu. Para yetmiyorsa buton `interactable = false`.
- `PlayerInteractor.OnDisable` küçük ek: kontrol kilitlenince ekranda **asılı kalan `[E]` ipucu temizlenir**.

### Adım 5c — Kurye teslimatı ✅ (Supermarket Simulator döngüsü)
- `DeliveryManager` (singleton, `KanBagi.Delivery`): siparişi coroutine ile bekletir, süre dolunca teslimat alanına koli düşürür.
  - `bool IsReady` (boxPrefab var mı), `bool QueueOrder(ItemData, int)`, `int PendingOrders`
  - `event Action<ItemData, int> OnOrderQueued` / `OnDelivered`
  - Inspector: `deliveryDelay` (test 8 sn), `dropHeight` 0.6, `scatterRadius` 0.35 (koliler üst üste binmesin), `deliveryPoint`
  - `OnDrawGizmosSelected` → teslimat alanı yeşil daire
- `PurchaseManager.TryBuy`: artık `InventoryManager.Add` YOK. **Para düşmeden önce `DeliveryManager.IsReady` kontrol edilir** (para boşa gitmesin), sonra `QueueOrder`.
- `DeliveryBox` (`RequireComponent(HoldableItem)`): `Content`, `Amount`, `IsEmpty`, `Fill(item, count)`, `GetLabel()`.
  - `Fill` içinde `HoldableItem.SetDisplayName(...)` çağrılır → **HeldItemUI'a dokunmadan** "Elinde: Cay Yapragi Kolisi (10)" yazar.
- `HoldableItem.SetDisplayName(string)` eklendi.
- `StorageRack` (`IInteractable`): elde dolu koli varsa `InventoryManager.Add(Content, Amount)` yapıp koliyi yok eder. İpucu: `"Koliyi Bosalt (10 Cay Yapragi)"`.
- `DeliveryNotificationUI`: sadece `OnOrderQueued`/`OnDelivered` dinler, birkaç saniye yazıp kaybolur (`messageText.enabled` ile — objeyi kapatmıyor ki coroutine ölmesin).

### Adım 5d — Upgrade sistemi + PC arayüzü yenilendi ✅
- `UpgradeData` (SO): `UpgradeName`, `Description`, `Cost`, `IsValid`. **Sadece veridir, ne yaptığını bilmez.**
- `UpgradeCatalog` (SO): PC'de listelenecek upgrade'ler. Asset: `GelistirmeKatalogu`.
- `UpgradeManager` (singleton, `KanBagi.Core`): `IsPurchased`, `CanAfford`, `TryPurchase`, `event Action<UpgradeData> OnUpgradePurchased`. Satın alınanlar `HashSet<UpgradeData>`.
- `UpgradeUnlocker` (sahne bileşeni): `requiredUpgrade` + `targets[]`. **SO ↔ sahne köprüsü** (ScriptableObject sahne objesi tutamaz).
  - ⚠️ **Hedefin kendisinde/altında DURMAMALI** — hedef kapalıyken kendisi de kapanır ve event'i duymaz. `Managers`'ta durur (aynı objeye birden fazla eklenebilir).
- `TabGroupUI`: sekme butonu + sayfa çiftleri. Dinleyiciler `Awake`'te, sekme seçimi `OnEnable`'da (panel her açıldığında varsayılan sekmeye döner).
  - ⚠️ Sekme butonlarının **Transition = None** olmalı, yoksa Button'un kendi renk geçişi aktif sekme rengini ezer.
- `UpgradeListUI` + `UpgradeEntryUI`: `ComputerUI`/`CatalogEntryUI`'ın kardeşleri. Alınmış upgrade'de buton `Alindi` olup pasifleşir.
- **PC paneli sıfırdan kuruldu — hiç anchor ayarı yok, her şey Layout Group ile.** Panel `Vertical Layout Group`, sayfalar `Flexible Height 1` (pasif sayfa layout'tan otomatik düşer).
- `RowPrefabBuilder` (**Editor script**): `Kan Bagi > Satir Prefablarini Olustur` menüsü `UrunSatiri` ve `UpgradeSatiri` prefab'larını **kod ile üretir** — ölçüler, fontlar ve script bağlantıları dahil.
  - Satır tasarımı değişecekse dosyanın üstündeki sabitleri değiştirip menüye tekrar tıkla.
  - ⚠️ Yeniden üretim bileşen kimliklerini değiştirir → `ComputerUI > Entry Prefab` ve `UpgradeListUI > Entry Prefab` alanları **yeniden bağlanmalı**.
- İlk upgrade: **Yeni Masa (100 TL)** → `Masa3` açılır. `Table.OnEnable` zaten `TableManager`'a kaydolduğu için **ekstra kod yok**.

### Adım 5e — Kahve, oralet ve menü çeşitliliği ✅ (FAZ 2 KAPANDI)
- `MenuManager` (singleton, `KanBagi.Shop`): dükkanın satabildiği içeceklerin **tek sahibi**.
  - `MenuEntry { Beverage, RequiredUpgrade }` — gerekli upgrade boşsa baştan menüde, doluysa alınınca girer
  - `AvailableBeverages`, `HasAnyBeverage`, `GetRandomBeverage()`, `IsOnMenu()`, `event Action OnMenuChanged`
  - `Start`'ta kurulur ve `UpgradeManager.OnUpgradePurchased`'a abone olur
- `Customer.ResolveOrder()`: `desiredOrder` **boşsa** menüden rastgele seçer. Inspector alanı override olarak kalır.
  - ⚠️ `Musteri` prefab'ında `Desired Order` **boş** olmalı, yoksa herkes aynı şeyi ister
  - Menü kilitli içecekleri içermediği için **müşteri yalnızca yapabildiğimiz şeyi ister**
- `ShopCatalog.CatalogEntry.requiredUpgrade` (opsiyonel): makinen yokken malzemesi listede görünmez → boşa para gitmez. `CatalogEntryUI` satırı `SetActive(false)` yapar, `ComputerUI` artık `OnUpgradePurchased`'a da abone.
- `ProgressBarUI`: `FindObjectsInactive.Include` — kilitli istasyonlar sonradan açılınca çubuk çalışır.
- **`TeaStove` tek satır değişmedi**: kahve makinesi ve oralet semaveri aynı script, farklı ItemData. (İsmi ileride `BeverageStation` olabilir.)
- Yeni upgrade'ler: **Kahve Makinesi 200 TL**, **Oralet Semaveri 100 TL** — her biri hem istasyonu açar hem içeceği menüye sokar.

### Adım 5f — Upgrade etkileri (stat iyileştirmeleri) ✅
- `TimedStation.DurationMultiplier` (varsayılan 1) + `SetDurationMultiplier(float)` — `StartWork` süreyi bununla çarpar. **`TeaStove` ve `Sink` tek satır değişmedi.**
- `StationSpeedBoost` (`KanBagi.Shop`): `requiredUpgrade` + `targetStations[]` + `durationMultiplier`. Pasif (kilitli) istasyonlar da atanabilir.
- `CustomerModifiers` (singleton): `PatienceMultiplier`, `MultiplyPatience(float)`, `event OnModifiersChanged`. Çarpanlar **çarpılarak birikir** → iki upgrade üst üste etki eder.
- `CustomerPatienceBoost` (`KanBagi.Customers`): upgrade alınınca çarpanı büyütür. `isApplied` bayrağı ile **iki kez uygulanmaz**.
- `Customer.GetEffectivePatience()` — sabır artık `patienceDuration × PatienceMultiplier`.
- Yeni upgrade'ler: **Sanayi Tipi Ocak 250 TL** (süre ×0.6), **Okey Takimi 150 TL** (masalara okey açar **+** sabır ×1.5).
- **Okey Takimi tek upgrade'e iki dinleyici**: `UpgradeUnlocker` objeleri açar, `CustomerPatienceBoost` sabrı artırır. Observer Pattern'in neden seçildiğinin örneği.
- ⚠️ Masa üstü dekor objelerinin **Box Collider'ı kaldırılmalı** — yoksa masaya bakınca ışını kesip servis ipucunu engeller.

### Adım 6a — Gün döngüsü ✅
- `DayCycleManager` (singleton, `KanBagi.Core`): gün **00:00 → 24:00 kesintisiz** akar; `openHour`/`closeHour` sadece dükkanın açık olduğu penceredir.
  - `CurrentDay`, `TimeOfDay` (0-24 ondalıklı), `DayProgress`, `IsShopOpen`, **`AcceptsNewCustomers`**, `GetClockLabel()`
  - `event`: `OnDayStarted`, `OnShopOpened`, `OnShopClosed`, `OnDayEnded`, `OnTimeChanged`
  - Inspector: `dayDurationMinutes` 6, **`nightTimeScale` 4** (gece hızlı akar, ölü bekleme olmaz — 1 yaparsan kapanır), `lastCallHours` 1
  - **Müşteri sistemini hiç tanımaz** — bağımlılık tek yönlü.
- `CustomerSpawner`: artık `List<Customer>` tutuyor. `ActiveCustomers`, `event OnCustomerSpawned` (6b için).
  - `CanSpawn()` → `AcceptsNewCustomers` (kapanışa 1 saat kala yeni müşteri yok)
  - `OnShopClosed` → içerideki **herkese `LeaveNow()`** (liste kopyası üzerinde dönülür)
- `Customer.LeaveNow()`: coroutine'leri durdurur, servis edilmediyse `LosePatience()`, `FinishDrink()` ile **yarım kalan bardağı kirli ve alınabilir bırakır** (yoksa oyuncunun asla temizleyemeyeceği kilitli bardak kalırdı).
- `Customer.WasServed` / `PaidAmount` — 6b'nin günlük özeti bunları okuyacak, yeni event gerekmedi.
- `ClockUI`: `Gun 3 - 14:30`, `(SON MUSTERI)` ve `(KAPALI)` etiketleri. Metin değişmediyse yeniden yazmaz (çöp üretmez).

### Adım 6b — Günlük özet paneli ✅
- `EconomyManager.OnMoneySpent` — `TrySpendMoney` **tüm harcamaların tek geçiş noktası** olduğu için malzeme + upgrade gideri tek event'ten yakalanıyor, hiçbir satın alma koduna dokunulmadı.
- `DailyStatsManager` (singleton, `KanBagi.Core`): `CustomersServed`, `CustomersLost`, `Revenue`, `Expenses`, `NetProfit`, `ReputationAtDayStart`, `ReputationChange`.
  - Müşterileri `CustomerSpawner.OnCustomerSpawned` → `customer.OnLeft` zinciriyle sayar; 6a'daki `WasServed`/`PaidAmount` sayesinde **yeni event gerekmedi**.
  - `OnDayStarted`'da sıfırlanır.
- `DayCycleManager`: `RequestNextDay()` + `IsWaitingForNextDay` + `waitForNextDayRequest`. **Gün numarası bekleme bitene kadar artmaz** → panel biten günü gösterir.
- `DailyReportUI`: `OnDayEnded`'a abone, paneli açar, `PlayerLock` ile oyuncuyu kilitler, PC ekranı açıksa kapatır. Net kâr yeşil/kırmızı, itibar `7  (+2)` biçiminde.
  - **Kök hep aktif, görseli yok**; açılıp kapanan `Icerik` (ekran karartması) çocuğu. Kökte görsel olsaydı ekranı sürekli kapatırdı.
  - `NormalizeLayout()`: prefab yanlış ebeveyne bırakılırsa panel köşede minik kalıyordu — script açılışta kendini Canvas'ın doğrudan çocuğu yapıp `SetAsLastSibling()` ile en öne alıyor ve rect'i geriyor.
- `ReportPanelBuilder` (**Editor script**): `Kan Bagi > Gunluk Ozet Panelini Olustur` → paneli kod ile üretir, iç bağlantılar hazır gelir. Kullanıcının tek işi prefab'ı Canvas'a sürüklemek.

### Adım 6c — Kayıt sistemi ✅
- `ISaveable`: `SaveKey`, `string CaptureState()`, `void RestoreState(string)`. **Durum JSON metni olarak taşınır** — `object` yerine string kullanıldı, çünkü `JsonUtility` polimorfizmi ve Dictionary'yi desteklemiyor. Her sistem kendi `JsonUtility` çevrimini yapar.
- `SaveManager` (singleton, **`[DefaultExecutionOrder(100)]`**): sahnedeki tüm `ISaveable`'ları bulur (`FindObjectsInactive.Include`), `Application.persistentDataPath/kanbagi_save.json` dosyasına yazar.
  - `Save()`, `Load()`, `HasSave`, `DeleteSave()`, `OnSaved`, `OnLoaded`
  - **Execution order 100 kritik**: tüm sistemlerin `Start`'ı bittikten sonra yükler, böylece kilit açan/etki uygulayan dinleyiciler hazır olur.
- `ISaveable` uygulayanlar: `EconomyManager` (para), `ReputationManager` (itibar), `InventoryManager` (stok), `UpgradeManager` (upgrade'ler), `DayCycleManager` (gün numarası).
- **ScriptableObject'ler asset adıyla kaydedilir**, yüklerken `ItemDatabase` / `UpgradeCatalog` üzerinden eşlenir.
  - ⚠️ Yeni `ItemData` eklediğinde **`UrunVeritabani`'na da eklemelisin**, yoksa kayıttan geri gelmez.
- ⚠️ **En kritik nokta:** `UpgradeManager.RestoreState` yükleme sonunda **`OnUpgradePurchased`'ı yeniden duyurur**. Yoksa kilitler kapalı kalır, menü büyümez, hız/sabır etkileri uygulanmaz.
- Otomatik kayıt **`OnDayStarted`**'a bağlı, `OnDayEnded`'a değil.
  - Sebep: gün sayacı oyuncu "Yeni Gun" dedikten **sonra** artıyor; gün sonunda kaydetmek diske hep bir önceki günü yazıyordu (test sırasında yakalanan bug).
  - Açılışta boşuna kayıt oluşmaz: 1. günün `OnDayStarted`'ı `DayCycleManager.Start` içinde, SaveManager abone olmadan önce tetiklenir.
- Saat kaydedilmez, **gün baştan (00:00) başlar**.
- PC ekranında **Kaydet** butonu + `Kaydedildi` bildirimi (`ComputerUI`).
- `SaveMenuItems` (Editor): `Kan Bagi > Kaydi Sil`, `Kan Bagi > Kayit Klasorunu Ac`.
- `SaveManager.Load` teşhis logu basar: `[SaveManager] Yuklendi: economy, reputation, ... | Kayitta YOK: ...`

---

## 6.5 FAZ 3 — MAHALLE VE EMLAK

### Adım 7a — Emlak çekirdeği ✅ (itibar ilk kez işlevsel)
- `PropertyData` (SO): `PropertyName`, `Description`, `PurchasePrice`, `DailyIncome`, **`RequiredReputation`**.
- `PropertyCatalog` (SO): liste + `Find(assetName)` (kayıt eşlemesi için).
- `PropertyManager` (singleton, `ISaveable`, `KanBagi.Core`):
  - `IsOwned`, `MeetsReputation`, `CanPurchase`, `TryPurchase`, `TotalDailyIncome`, `OwnedCount`, `event OnPropertyPurchased`
  - `OnDayStarted`'da kirayı `EconomyManager.AddMoney` ile kasaya yazar → **aldığın gün değil, ertesi gün yatar**
  - `LastRentPaid` — o günün kirası. **Event yerine değer** tutuluyor ki "önce kim dinledi" yarışı olmasın.
- `DailyStatsManager.RentIncome` → `PropertyManager.LastRentPaid`'i okur. `NetProfit = Revenue + RentIncome - Expenses`.
- PC'de **3. sekme `EMLAK`** — `TabGroupUI` genişletildi, `PropertyListUI` + `PropertyEntryUI`.
  - İtibar yetmiyorsa butonda fiyat yerine **`Itibar 35 gerekli`** yazar (oyuncu neden alamadığını görür).
- `RowPrefabBuilder` artık `EmlakSatiri` prefab'ını da üretiyor (butonu daha geniş: itibar şartı yazısı sığsın).
- **Test sırasında yakalanan 2 bug:**
  1. `PropertyManager.RestoreState` katalog boşken `owned.Clear()` yapıyordu → veri siliniyordu. Artık katalog yoksa **hiçbir şeye dokunmuyor**.
  2. **`LastRentPaid` kaydedilmiyordu.** Açılışta gün 1 başlarken mülkler henüz yüklenmemiş olduğu için kira 0 hesaplanıyor; bu değer geri yüklenmezse devam edilen günün özetinde kira 0 görünüyordu. Artık kaydedilip geri yükleniyor (kira ikinci kez **ödenmez**, sadece rapora yazılır).

### Adım 7b — Fiziksel mahalle ✅
- `Sokak` (Plane, z = -5 … -15) mevcut `Zemin`in devamı — kapı yok, yürüyerek çıkılıyor (bilinçli tercih, kapı sonra eklenecek).
- Üç bina (Cube, scale 3): `BakkalBinasi`, `TerziBinasi`, `KirathaneBinasi` — her birinde `PropertyBuilding`.
- `PropertyBuilding` (`IInteractable`, `KanBagi.Neighborhood`): mülkü **yerinde** satın alma.
  - İpuçları: `Kosedeki Bakkal - 800 TL` / `Itibar 10 gerekli` / `800 TL gerekli` / `Senin dukkanin - Gunluk 60 TL`
  - PC ekranıyla **aynı `PropertyManager.TryPurchase`**'ı çağırır → tek doğruluk kaynağı, iki arayüz.
  - ⚠️ Collider ile **aynı objede** olmalı (PlayerInteractor tek ışın atıp `hit.collider` üzerinde arar).
- `PropertySignUI`: binanın **duvarına yapışık** SATILIK tabelası. Billboard YOK (sabit durur). Üstünde mülk adı, fiyat, günlük gelir, gerekli itibar. **Satın alınınca tabela tamamen kaybolur.** Kayıttan yüklenmiş mülkün tabelası hiç görünmez.
- `PropertyVisualSwitch`: opsiyonel görsel değişimi (`unownedObjects` / `ownedObjects`).
- `SignPrefabBuilder` (**Editor**) iki menü sunar:
  - `Kan Bagi > Emlak Tabelasi Prefabini Olustur` → levha + iki satır 3D yazı, collider yok
  - **`Kan Bagi > Tabelalari Binalara Yerlestir`** → sahnedeki her binanın **Renderer.bounds'undan** ön duvarı hesaplayıp tabelayı oraya yapıştırır, binanın çocuğu yapar, **parent ölçeğini ters çevirerek** yazının büyümesini engeller, `Building` alanını bağlar, eski tabelayı siler.
  - ⚠️ **Elle koordinat girmeye çalışmayın** — tabela konumu bu komutla hesaplanıyor. Yükseklik `AttachSign` içindeki `bounds.size.y * 0.55f` ile ayarlanır.

---

## 6.6 FAZ 4 — İLLEGAL İŞLER

### Adım 8a — Gizli kumarhane ✅
- `UpgradeData.requiredReputation` (0 = şart yok) + `UpgradeManager.MeetsReputation`. `UpgradeEntryUI` fiyat yerine **`Itibar 50 gerekli`** yazar; `UpgradeListUI` artık `OnReputationChanged`'a da abone (itibar artınca kendiliğinden açılır).
- `GamblingRoundResult` (readonly struct): `Won`, `Stake`, `Payout`, `Net`.
- `GamblingTable` (`IInteractable`, `KanBagi.Illegal`): **sadece dükkan kapalıyken** (`IsOpen => !DayCycleManager.IsShopOpen`).
  - Inspector: `betAmount` 100, `winChance` 0.45, `payoutMultiplier` 2, `suspicionPerRound` 3
  - `NightNet` her gece (`OnShopClosed`) sıfırlanır, `event OnRoundPlayed`
  - İpuçları: `Kumar Oyna (100 TL)` / `Gunduz kumar oynanmaz` / `100 TL gerekli`
- `SuspicionManager` (singleton, `ISaveable`): `CurrentSuspicion`, `SuspicionRatio`, `Add/Reduce`, gün başında `dailyDecay` kadar düşer. **8c'de polis bunu okuyacak.**
- `GamblingSignUI`: masanın üstünde 3D tabela — `BAHIS 100 TL` / `KAZANDIN +100 TL` / `Gece: -200 TL`. Gündüz `GUNDUZ KAPALI`.
- `GamblingSignBuilder` (**Editor**): `Kan Bagi > Kumar Tabelasini Olustur ve Yerlestir` — masanın ölçülerinden hesaplayıp üstüne koyar. **`SignYawDegrees` = 180** (kapı tarafına bakar; tezgahın arkasına dolanmak gerekmesin).
- `GizliKumarhaneUpgrade` (1500 TL + **itibar 50**) → `UpgradeUnlocker` masayı açar.
- **Muhasebe tuzağı çözüldü:** bahis `TrySpendMoney`'den geçtiği için `Expenses`'a yazılıyordu; `DailyStatsManager.HandleGamblingRound` bahsi giderden **geri çıkarıp** `GamblingNet`'e yazıyor → çift sayım yok. `NetProfit = Revenue + RentIncome + GamblingNet - Expenses`.
- **Yakalanan soft-lock:** `GunlukOzetPaneli` sahneden silinince `OnDayEnded`'ı dinleyen kalmıyor, `DayCycleManager` "Yeni Gun" bekleyip **oyunu sessizce kilitliyordu** (otomatik kayıt da durmuş görünüyordu). Artık `OnDayEnded == null` ise uyarı basıp günü kendiliğinden devrediyor.

### Adım 8b — Mafya ve haraç ✅
- `MafiaManager` (singleton, `ISaveable`, `KanBagi.Illegal`):
  - **Baraj:** `requiredReputation` 30 **VE** `requiredMoney` 2000 — gün başında kontrol edilir, bir kez tetiklenir
  - **Zamanlama:** baraj aşılınca hemen gelmez → `firstVisitDelayDays` 2 sonra, ayrıca `earliestVisitDay` 3'ten önce asla. Sonraki ziyaretler `visitIntervalDays` 3 günde bir.
  - `PayDemand()` → `TrySpendMoney` + tahsildar yok olur. **Ödenmezse ertesi gün başında reddedilmiş sayılır**: itibar −5, şüphe +10, `refusalCount++` → sonraki haraç `baseDemand × 1.5^refusalCount`
  - **Ayrı "reddet" tuşu yok** — görmezden gelmek reddetmektir (bilinçli tasarım)
  - `event OnDemandArrived / OnDemandPaid / OnDemandRefused`, `PaidToday`
- `MafiaCollector` (`IInteractable`): haraç aktifken `MafiaManager` tarafından `spawnPoint`'te üretilir, ödenince/reddedilince yok edilir. Kayıttan yüklenince **hâlâ ödenmemişse yeniden belirir**.
- `MafiaSignUI` + `MafiaPrefabBuilder` (**Editor**): `Kan Bagi > Tahsildar Prefabini Olustur` → kapsül gövde + üstünde `HARAC` / `500 TL` tabelası, hepsi bağlı.
  - ⚠️ **Tahsildar tabelasında `Billboard` VAR** (insan, etrafından dolaşılır) — duvara asılı emlak tabelalarında **YOK** (binaya sabit). Billboard varken levha yazının **arkasında** (+Z) olmalı, sabit tabelada **önünde** (−Z). Karıştırılırsa yazı görünmez.
- Günlük özete **"Harac"** satırı. Haraç da `TrySpendMoney`'den geçtiği için `Expenses`'tan düşülüp kendi satırına yazılır (kumar ile aynı çift-sayım çözümü).
  - `NetProfit = Revenue + RentIncome + GamblingNet - Expenses - ProtectionPaid`
- **Test sırasında yakalanan bug:** mafya barajı aştığın **gün** kapıya dayanıyordu (oyun daha oynanmadan). `firstVisitDelayDays` + `earliestVisitDay` ile çözüldü.

### Adım 8c — Polis baskını ✅ (şüphenin ilk gerçek sonucu)
- **Gizleme mekaniği** `GamblingTable`'a eklendi: `IsHidden` (varsayılan **true** — güvenli taraf), `SetHidden`, `coverVisual` + `exposedVisuals[]`, `event OnHiddenChanged`.
  - **E tuşu bağlama göre anlam değiştiriyor:** gündüz kumar oynanamadığı için E **örtüyü** açar/kapatır; gece örtü kapalıysa önce açar, açıksa oynar. Ayrı bir tuş/obje gerekmedi.
  - `exposedVisuals`'a masanın tabelası konur, yoksa örtülü masanın tepesinde "BAHIS 100 TL" parlar.
- `PoliceManager` (singleton, `KanBagi.Illegal`) — **kayıt edilmez**, tamamen gün içi durum:
  - Gün başında zar: `SuspicionRatio < minimumSuspicionRatio` (0.2) ise hiç gelmez; değilse ihtimal `ratio × maxRaidChance` (0.6). `earliestRaidDay` 4'ten önce baskın yok.
  - Akış: dükkan açıkken rastgele bir saat → **`OnRaidWarning`** (20 sn süre) → memur gelir (`OnOfficerArrived`) → 12 sn denetim → `OnRaidResolved(busted, fine)`
  - Yakalanınca: para cezası (parası yetmezse elindeki kadarı), itibar −10, **şüphe sıfırlanır** (dosya kapandı). Temiz geçerse şüphe −15.
  - `FinePaidToday` — günlük özet bunu okur. Ceza `TrySpendMoney`'den geçtiği için zaten `Expenses`'ta; **`NetProfit`'te tekrar düşülmez**, satır sadece bilgi amaçlı (kumar/haraçtan farklı).
- `PoliceOfficer`: bilinçli olarak boş `Interact()` — memurla pazarlık yok, iş masayı zamanında örtmekti.
- `PoliceNotificationUI`: uyarı (sarı) / memur geldi (kırmızı) / sonuç mesajları.
- `PolicePrefabBuilder` (**Editor**): `Kan Bagi > Polis Prefabini Olustur` + **`Kan Bagi > Kumar Ortusunu Olustur ve Bagla`** (örtüyü üretip `coverVisual` alanına kendisi bağlar).
- `DayCycleManager`'a `OpenHour` / `CloseHour` getter'ları eklendi (baskın saatini seçmek için).

---

## 7. SAHNE KURULUMU (`Assets/Scenes/SampleScene.unity`)

| Obje | Konum | Üzerindeki script'ler |
|---|---|---|
| `Zemin` (Plane) | (0,0,0) | **NavMeshSurface** (bake edilmiş) |
| `Player` (Empty) | (0, 1.1, 0) | CharacterController, PlayerController, PlayerInteractor, PlayerHands, PlayerLock |
| └ `Main Camera` | (0, 0.6, 0) | — (child) |
| `Managers` (Empty) | — | EconomyManager, ReputationManager, TableManager, InventoryManager, PurchaseManager, DeliveryManager, UpgradeManager, MenuManager, CustomerModifiers, StationSpeedBoost, CustomerPatienceBoost, DayCycleManager, DailyStatsManager, SaveManager, PropertyManager, SuspicionManager, MafiaManager, PoliceManager, **5 × UpgradeUnlocker** (YeniMasa→Masa3, KahveMakinesi→KahveMakinesi, Oralet→OraletSemaveri, Okey→OkeyTakimi1/2/3) |
| `EventSystem` | — | **Input System UI Input Module** (butonlar bunsuz çalışmaz) |
| `Canvas` | — | InteractionPromptUI, HeldItemUI, ProgressBarUI, **ComputerUI** |
| ├ `MoneyText` | sol üst (Pos Y -20) | MoneyUI |
| ├ `ReputationText` | Pos Y -55 | ReputationUI |
| ├ `StockText` | Pos Y -90 | StockUI (tracked: CayYapragi) |
| ├ `PromptText` | orta-alt | — |
| ├ `HeldItemText` | sol alt | — |
| ├ `BildirimText` | üst-orta | DeliveryNotificationUI |
| ├ `SaatText` | sağ üst | ClockUI |
| ├ `PolisUyariText` | üst-orta (Pos Y -90) | PoliceNotificationUI |
| ├ `GunlukOzetPaneli` (**Canvas'ın son çocuğu**) | tam ekran | DailyReportUI (kök hep aktif) |
| &nbsp;&nbsp;└ `Icerik` → `Panel` (560×460, VLG) | orta | 7 satır + `YeniGunButton` |
| ├ `ProgressBar` → `Dolum` (Image/Filled) + `ProgressLabel` | orta-alt | — |
| └ `BilgisayarPaneli` (700×450, **Vertical Layout Group**, sahnede PASİF) | orta | TabGroupUI |
| &nbsp;&nbsp;├ `UstBar` (HLG, pref. height 44) → `Baslik`, `KayitDurumText`, `KasaText`, `KaydetButton`, `KapatButton` | — | — |
| &nbsp;&nbsp;├ `SekmeBar` (HLG, pref. height 50) → `MalzemeSekmeButton`, `GelistirmeSekmeButton`, `EmlakSekmeButton` | — | butonlarda Transition = None |
| &nbsp;&nbsp;├ `MalzemeSayfasi` (VLG, flexible height 1) | — | ComputerUI'ın `entryContainer`'ı |
| &nbsp;&nbsp;├ `UpgradeSayfasi` (VLG, flexible height 1) | — | UpgradeListUI |
| &nbsp;&nbsp;└ `EmlakSayfasi` (VLG, flexible height 1) | — | PropertyListUI |
| `BardakRafi` (Cube) | (-2, 0.5, 2) | GlassShelf (stok 10, prefab: CayBardagi) |
| `CayOcagi` (Cube) | (0, 0.5, 3) | TeaStove (Cay + CayYapragi × 1) |
| `KahveMakinesi` (Cube) — **başta pasif** | (-1, 0.5, 3) | TeaStove (Kahve + KahveCekirdegi × 1) |
| `OraletSemaveri` (Cube) — **başta pasif** | (1, 0.5, 3) | TeaStove (Oralet + OraletTozu × 1) |
| `Lavabo` (Cube) | (2, 0.5, 3) | Sink (washDuration 2.5) |
| `Masa1` (Cube) | (-2.5, 0.4, -1) | Table (IInteractable — collider aynı objede olmalı) |
| `Masa2` (Cube) | (2.5, 0.4, -1) | Table |
| `Masa3` (Cube) — **başta pasif** | (0, 0.4, -2) | Table (Yeni Masa upgrade'i açar; NavMesh **açıkken** bake edildi) |
| `OkeyTakimi1/2/3` — masaların child'ı, **başta pasif** | yerel (0.3, 0.5, 0) | — (Box Collider **yok**) |
| `GirisNoktasi` (Empty) | (0, 0, -4.5) | — (spawn + exit) |
| `Spawner` (Empty) | — | CustomerSpawner |
| `YonetimPC` (Cube) | (2, 0.5, 2) | ComputerStation (catalog: MalzemeKatalogu) |
| `TeslimatNoktasi` (Empty) | (2, 0, -3.5) | — (kapının yanı, koliler buraya düşer) |
| `Mahalle` (Empty) | — | — |
| ├ `Sokak` (Plane) | (0, 0, -10), scale (1.5,1,1) | — |
| ├ `BakkalBinasi` (Cube, scale 3) | (-4, 1.5, -9) | PropertyBuilding (BakkalMulk) + tabela child'ı |
| ├ `TerziBinasi` (Cube, scale 3) | (0, 1.5, -12.5) | PropertyBuilding (TerziMulk) |
| └ `KirathaneBinasi` (Cube, scale 3) | (4, 1.5, -9) | PropertyBuilding (KirathaneMulk) |
| `Depo` (Cube) | (-2, 0.5, 3.5) | StorageRack |
| `KumarMasasi` (Cube) — **başta pasif** | (-4, 0.4, 3.5) | GamblingTable + tabela child'ı (Gizli Kumarhane upgrade'i açar) |
| `MafyaNoktasi` (Empty) | (1.5, 0.9, -4) | — (tahsildar burada belirir, y=0.9 kapsül yere bassın diye) |
| `PolisNoktasi` (Empty) | (-1.5, 0.9, -4) | — (memur burada belirir) |

**Prefab'lar:**
- `CayBardagi` (Cylinder 0.08/0.05/0.08 + child `Sivi` + HoldableItem + TeaGlass)
- `Musteri` (Capsule + NavMeshAgent + Customer + OrderBubbleUI + child `SiparisBalonu` (3D TMP + Billboard)) — **Desired Order BOŞ** (menüden rastgele seçilir), Drink Duration 6, Patience 25
- `UrunSatiri`, `UpgradeSatiri`, `EmlakSatiri` — **elle kurulmaz**, `Kan Bagi > Satir Prefablarini Olustur` menüsü üretir
- `GunlukOzetPaneli` — **elle kurulmaz**, `Kan Bagi > Gunluk Ozet Panelini Olustur` menüsü üretir
- `EmlakTabelasi` — **elle kurulmaz/yerleştirilmez**, `Kan Bagi > Emlak Tabelasi Prefabini Olustur` + `Kan Bagi > Tabelalari Binalara Yerlestir`
- `KumarTabelasi` — **elle kurulmaz**, `Kan Bagi > Kumar Tabelasini Olustur ve Yerlestir`
- `Tahsildar` — **elle kurulmaz, sahneye elle konmaz**, `Kan Bagi > Tahsildar Prefabini Olustur`; MafiaManager runtime'da üretir/yok eder
- `PolisMemuru` — aynı şekilde `Kan Bagi > Polis Prefabini Olustur`; PoliceManager runtime'da üretir/yok eder
- `KumarMasasi > Ortu` — `Kan Bagi > Kumar Ortusunu Olustur ve Bagla` menüsü üretip bağlar
- `Koli` (Cube 0.35/0.3/0.35 + **Rigidbody** + HoldableItem + DeliveryBox) — Rigidbody olmazsa koli havada asılı kalır

**ScriptableObject'ler:**
| ItemData | Tür | SellPrice | PurchaseCost | PrepareDuration |
|---|---|---|---|---|
| `Cay` | Beverage | 15 | 3 | 3 |
| `Kahve` | Beverage | 25 | 0 | 5 |
| `Oralet` | Beverage | 18 | 0 | 2 |
| `CayYapragi` | Consumable | 0 | 2 | 0 |
| `KahveCekirdegi` | Consumable | 0 | 4 | 0 |
| `OraletTozu` | Consumable | 0 | 3 | 0 |

- `MalzemeKatalogu` (ShopCatalog) — CayYapragi ×10 (20 TL), KahveCekirdegi ×10 (40 TL, kilitli), OraletTozu ×10 (30 TL, kilitli)
- `GelistirmeKatalogu` (UpgradeCatalog) — YeniMasaUpgrade (100), KahveMakinesiUpgrade (200), OraletUpgrade (100), SanayiOcagiUpgrade (250), OkeyTakimiUpgrade (150), GizliKumarhaneUpgrade (1500 + **itibar 50**)
- `UrunVeritabani` (ItemDatabase) — 6 ItemData'nın tamamı (kayıt sistemi için, **yeni ürün eklenince buraya da eklenmeli**)
- `BakkalMulk` (800 TL / 60 / itibar 10), `TerziMulk` (1200 / 90 / 20), `KirathaneMulk` (2000 / 160 / 35)
- `EmlakKatalogu` (PropertyCatalog) — üç mülkün tamamı (**kayıt eşlemesi buradan yapılır, boş bırakılamaz**)

> ⚠️ **Test notu:** `Managers > EconomyManager > Starting Money` test için **2000** yapıldı. Denge ayarına gelince **250**'ye geri alınacak.

---

## 8. ŞU AN NEREDE KALDIK

**Adım 8c bitti ve test edildi.** Şüphe biriktikçe polis baskın yapıyor: uyarı geliyor, oyuncunun masayı örtmek için ~20 saniyesi oluyor, örtmezse para cezası + itibar kaybı, örterse temiz geçip şüphe düşüyor. Faz 4'ün üç ayağı (kumarhane, mafya, polis) birbirine bağlandı.

**Adım 8b bitti ve test edildi.** Mafya itibar 30 + servet 2000 barajını aşınca haberdar oluyor, birkaç gün sonra tahsildar kapıya geliyor; ödenince para düşüyor ve adam gidiyor, ödenmeyince ertesi gün itibar/şüphe cezası uygulanıp haraç ×1.5 büyüyor. Günlük özette "Harac" satırı doğru, kayıt/yükleme sorunsuz.

**Adım 8a bitti ve test edildi — FAZ 4 BAŞLADI.** Gizli kumarhane itibar 50 + 1500 TL ile açılıyor, sadece geceleri oynanıyor, tabela sonucu ve gecelik hesabı gösteriyor, günlük özette "Kumar" satırı doğru (bahisler giderde çift sayılmıyor), şüphe birikiyor ve kaydediliyor.

**Adım 7b bitti ve test edildi — FAZ 3 KAPANDI.** Dükkandan çıkıp sokağa yürünüyor, binaların duvarındaki SATILIK tabelaları bilgileri gösteriyor, mülk yerinde E ile satın alınıyor, alınınca tabela kayboluyor; PC ve sokak arayüzleri senkron.

**Adım 7a bitti ve test edildi — FAZ 3 BAŞLADI.** PC'de EMLAK sekmesinden mülk alınıyor, itibar barajı çalışıyor, kira ertesi günden itibaren her gün kasaya yatıyor, günlük özette "Kira geliri" görünüyor ve hepsi kayıttan doğru geri geliyor.

**Adım 6c bitti ve test edildi — FAZ 2 ALTYAPISI TAMAM.** Para, itibar, stok, alınan upgrade'ler ve gün numarası diske kaydediliyor; oyun açılınca kaldığı yerden devam ediyor, kilitli objeler ve menü doğru şekilde geri geliyor.

**Adım 6b bitti ve test edildi.** Gün 24:00'te bitince özet paneli açılıyor, oyuncu kilitleniyor; servis edilen/kaçırılan müşteri, ciro, gider, net kâr ve itibar değişimi doğru görünüyor; "Yeni Gun" ile yeni gün başlıyor ve sayaçlar sıfırlanıyor.

**Adım 6a bitti ve test edildi.** Gün 00:00'dan 24:00'a kesintisiz akıyor, gece hızlı geçiyor, 21:00'den sonra yeni müşteri gelmiyor, 22:00'de içerideki herkes kalkıp çıkıyor (yarım kalan bardak masada kirli kalıyor), 24:00'te yeni gün başlıyor.

**Adım 5f bitti ve test edildi.** Sanayi tipi ocak tüm içecek istasyonlarını hızlandırıyor (çay 3→1.8 sn, kahve 5→3 sn), okey takımı hem masalarda beliriyor hem müşteri sabrını 25→37.5 sn'ye çıkarıyor. Okey kutuları servis ışınını engellemiyor.

**Adım 5e bitti ve test edildi — FAZ 2 KAPANDI.** Müşteriler menüden rastgele sipariş veriyor (çay/kahve/oralet), her müşteri **sadece kendi istediğini** kabul ediyor, makinesi olmayan içecek menüye girmiyor, malzemesi kilitli kalıyor.

**Adım 5d bitti ve test edildi.** PC arayüzü sıfırdan, Layout Group tabanlı kuruldu — hizalama sorunu yok. `MALZEMELER` / `GELISTIRME` sekmeleri çalışıyor. Yeni Masa 100 TL'ye alınıyor, buton `Alindi` oluyor, `Masa3` sahnede beliriyor ve müşteri gelip oturuyor.

**Adım 5c bitti ve test edildi.** Sipariş artık ışınlanmıyor: PC'den alınıyor → "yolda" bildirimi → süre sonunda kapının yanına koli düşüyor → oyuncu koliyi alıp `Depo`'ya götürüp boşaltıyor → stok artıyor.

**Adım 5b bitti ve test edildi.** `YonetimPC`'ye E ile bakılınca ekran açılıyor, imleç serbest kalıyor, oyuncu kilitleniyor; 20 TL'ye 10 ölçek çay yaprağı alınıyor, stok ve para anında güncelleniyor, para yetmeyince Satın Al butonu pasifleşiyor, ESC/Kapat ile çıkılıyor.

**Adım 5a bitti ve test edildi.** Çay artık bedava değil: her bardak 1 ölçek `CayYapragi` harcıyor, sol üstte stok görünüyor, stok bitince ocak çalışmıyor ve **bardak oyuncunun elinde kalıyor.**

**Adım 4b bitti ve test edildi — FAZ 1 TAMAMEN KAPANDI.** Oyuncu çayı hazırlayıp müşteriye servis ediyor, para ve itibar kazanıyor, müşteri içip gidiyor, masada kalan kirli bardağı oyuncu lavaboya götürüp rafa iade ediyor. Bekletilen müşteri itibar düşürüyor.

Şu anda çalışan tam döngü:
```
BardakRafi ──E──> [Elde bos bardak] ──E──> CayOcagi (3sn) ──E──> [Dolu cay]
     ▲                                                                │
     │                                                                E (Masa)
     │                                                                ▼
     │                                              +15 TL, +1 Itibar / musteri icer (6sn)
     │                                                                │
     └──── Lavabo (2.5sn) <──── [Masada kirli bardak] <───────────────┘

Musteri: giris → masaya yuru → otur → "Cay" balonu → SERVIS → icer → cikar
         (servis edilmezse: sabir biter → -1 itibar → cikar)
```

**Faz 2 bitti.** Tüm döngü çalışıyor:
```
PC'den siparis → kurye koliyi kapiya birakir → depoya tasi → stok
     ↓                                                        ↓
  para                                    Raf → Bardak → Ocak/Makine → Dolu icecek
     ↑                                                        ↓
  +TL, +itibar ←──── MASA (servis) ←───────────────────────────┘
     ↓                     ↓
  GELISTIRME        kirli bardak → Lavabo → Raf
  (masa, makine)
```
Faz 2'nin tamamı bitti: stok, PC, kurye, upgrade'ler (obje açma **ve** stat iyileştirme), menü çeşitliliği.

**Eksik olan iki temel altyapı:**
1. **Kalıcılık yok** — oyunu kapatınca para, stok ve upgrade'ler sıfırlanıyor.
2. **Zaman kavramı yok** — gün/gece yok. Faz 3'ün "pasif gelir"i ve Faz 4'ün "haraç"ı ikisi de periyodik bir zaman birimi ister.

---

**Master prompt'taki dört fazın tamamı iskelet olarak ayakta.** Oyun baştan sona oynanabiliyor: çay sat → büyü → mahalleyi al → geceleri kumar → mafya ve polisle başa çık.

Bundan sonrası **cila ve denge**. Öncelik sırasıyla:

---

## 9. SIRADAKİ ADIM — 9A: DENGE VE İLK GERÇEK OYUN

**Amaç:** Test için şişirilmiş değerleri gerçek oyuna çevirmek ve baştan sona oynanabilir bir denge kurmak.

Planlanan (henüz onaylanmadı):
1. **Test değerlerini geri al:** `Starting Money` 2000/5000 → **250**, `Starting Reputation` 40/60 → **0**, `Day Duration Minutes` → **6**, `Delivery Delay` → **20**, PoliceManager test değerleri → gerçek değerler.
2. Sıfırdan oyna, her fazın **ne kadar sürdüğünü ölç**: ilk upgrade kaçıncı gün alınabiliyor? Kumarhane (itibar 50) kaçıncı gün? Mafya barajı (itibar 30 + 2000 TL) ne zaman?
3. Tıkanan yerleri düzelt — fiyatlar, sabır, müşteri sıklığı, kira gelirleri.
4. `MoneyTestInteractable` benzeri kalıntı yok; ama `RowPrefabBuilder`/`ReportPanelBuilder`/`SignPrefabBuilder`/`GamblingSignBuilder`/`MafiaPrefabBuilder`/`PolicePrefabBuilder` arasında **kopyala-yapıştır kod** birikti — ortak bir `SignFactory` editor yardımcısına çıkarılabilir (isteğe bağlı temizlik).

**Sonraki fikirler (henüz planlanmadı):** kapı objesi ve dükkan içi/dışı ayrımı, müşteri çeşitliliği (farklı sabır/bahşiş), mafya ile işbirliği kolu, ses/müzik, ana menü.

---

## 10. SONRAKİ FAZLAR (Master Prompt'tan)

### Faz 2 — Bilgisayar, Stok, Dükkan Gelişimi
- **Yönetim PC'si**: dükkanda etkileşilen bilgisayar arayüzü (Supermarket Simulator mantığı)
- **Stok siparişi**: çay, şeker, kahve gibi sarf malzemeleri azalınca PC'den sipariş; kurye kapıya bırakır, oyuncu alıp depoya dizer
- **Upgrade'ler**: yeni masalar, daha hızlı ocaklar, kahve makineleri; okey takımı / iskambil / domino gibi müşteri oyalayan masa eşyaları; dükkanı genişletme

### Faz 3 — Mahalle ve Emlak
- Mahalledeki boş/satılık dükkanları satın alma, pasif gelir, farklı işletmelere dönüştürme

### Faz 4 — İllegal İşler, Mafya, Polis
- **Gizli kumarhane**: dükkan belli seviyeye gelince arka odada kumar; yüksek kazanç, yüksek risk
- **Mafya**: ilk günden gelmez — belli bir **İtibar + servet barajı** aşılınca haraç/racon kesmeye gelir
- **Polis**: rastgele "rutin kontrol"; arkada kumar varsa veya masada aranan adam varsa oyuncu bunları gizlemek zorunda

---

## 10.5 GIT / GITHUB DURUMU

| | |
|---|---|
| Repo (hedef) | `https://github.com/MetinKizil/Kahvehane_Simulation` |
| Yerel repo kökü | `D:\KAHVE DENEME\KAHVEHANEDENEME` (Unity proje klasörü) |
| Git kimliği | `MetinKizil` / `metinkizil210@gmail.com` |
| Kurulu araçlar | Git 2.55 + GitHub CLI 2.101 (winget ile kuruldu, PATH'e elle eklenmesi gerekebilir) |

**Durum:** 3 commit yerelde hazır ama **henüz push edilmedi** — `gh auth login` cihaz-kodu doğrulama maili gelmediği için giriş yapılamadı.
```
f329349  Adim 8c: polis baskini ve gizleme mekanigi
f9dc2d3  Adim 8b: mafya ve harac sistemi
7e6d8e1  Kan Bagi - Faz 1-2-3 ve Faz 4 basi
```

**Kurallar:**
- Her tamamlanıp test edilen adımdan sonra DEVAM.md güncellenir, sonra commit + push.
- ⚠️ **Commit mesajlarına Claude imzası (`Co-Authored-By`) EKLENMEZ**, PR açıklamalarına "Generated with Claude Code" yazılmaz. Kullanıcının açık talebi.
- Commit mesajları Türkçe ama ASCII (şapkasız).
- Giriş çözümü için öneri: tarayıcıda github.com'a girip **sonra** `gh auth login` çalıştırmak, ya da telefondan `github.com/login/device`, ya da `repo` yetkili Personal Access Token ile `gh auth login --with-token` (token kullanıcı kendi terminaline girer, sohbete yazılmaz).
- PowerShell oturumunda PATH tazelemek gerekebilir:
  `$env:Path = [System.Environment]::GetEnvironmentVariable("Path","Machine") + ";" + [System.Environment]::GetEnvironmentVariable("Path","User")`

---

## 11. YENİ SOHBETTE İLK YAPILACAK

1. Bu dosyayı oku.
2. `Assets/_Project/Scripts/` altındaki kodları oku — mevcut API ve stile uy.
3. **Adım 9a planını anlat, onay al**, sonra kod yaz.

### Bu projede öğrenilen dersler (tekrar aynı hataya düşme)
- **UI'ı elle kurdurma.** Panel/satır/tabela gerekiyorsa `Assets/_Project/Scripts/Editor/` altına bir builder yaz, menüden üretilsin. Elle kurulan ilk PC paneli üst üste bindi ve baştan yapıldı.
- **Sahne objelerine elle koordinat verme.** Tabela yerleştirme üç denemede de yanlış çıktı; çözüm hedefin `Renderer.bounds`'undan hesaplayan bir Editor komutu oldu.
- **Billboard ayrımı:** insan üstündeki tabelalarda Billboard VAR (levha yazının arkasında, +Z), duvara/masaya sabit tabelalarda YOK (levha önde, −Z). Karıştırılırsa yazı görünmez veya ters döner.
- **Prefab yeniden üretilince** o prefab'ı referans alan Inspector alanları kopar → yeniden bağlanmalı; sahnedeki kopyalar silinip yeniden konmalı.
- **Panel kökü hep aktif kalmalı** (kapalı obje event dinleyemez); açılıp kapanan şey içerik çocuğu olmalı.
- **`TrySpendMoney`'den geçen her özel harcama** (kumar bahsi, haraç) `Expenses`'a da yazılır → kendi satırında gösterilecekse giderden düşülmeli, yoksa çift sayılır.
- **Gün sonu bekleyen sistemler** (`OnDayEnded` dinleyicisi) yoksa oyun sessizce kilitlenir — guard eklendi ama benzer beklemelerde dikkat.
4. Kod bittikten sonra Unity Editor talimatlarını adım adım ver.
