# Установка
1. Установить ".NET Core 6" https://learn.microsoft.com/ru-ru/dotnet/core/install/
2. Загрузить и распаковать релиз https://github.com/immisterio/TSApi/releases в папку /opt/tsapi
3. Запустить "dotnet TSApi.dll -d=/opt/tsapi"

# Параметры
1. Настройки TSApi меняються в settings.json
```
{
  "port": 8090,
  "IPAddressAny": true,
  "worknodetominutes": 4,
  "maxiptoIsLockHostOrUser": 10,
  "AuthorizationRequired": true,       // access user to usersDb.json
  "KnownProxies": [                    // white ip to https://www.cloudflare.com/ips-v4
    {
      "ip": "ip_adress",
      "prefixLength": 24
    }
  ]
}
```
2. Список пользователей редактируется в usersDb.json
```
{
  "ts": {
    "login": "ts",
	"domainid": "ogurchik", // domain authorization - (ogurchik.tsapi.io - use dl/master) || ogurchik.117.tsapi.io (117 - use dl/117)
	"maxiptoIsLockHostOrUser": 10, // override settings.json
    "allowedToChangeSettings": true, // you can change settings
  },
  "ts2": {
    "login": "ts2",
	"passwd": "test",
	"domainid": null, // login and password authorization - tsapi.io
	"torPath": "117", // use dl/117/TorrServer-linux-amd64
	"shutdown": true, // allow server shutdown
    "allowedToChangeSettings": false, // can't change settings
  }
}
```
# Crontab
```
*/5 *   *   *   *    curl -s "http://127.0.0.1:8090/cron/updateusersdb"
*   *   *   *   *    curl -s "http://127.0.0.1:8090/cron/checkingnodes"
```

# Запуск на домене
1. В settings.json ставим false для IPAddressAny
2. В /etc/nginx/sites-enabled кидаем nginx.conf

