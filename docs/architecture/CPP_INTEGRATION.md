# Native boundary

`native/hostfall-core` — C++20 static library, не использует Unity API.
DeviceId = uint32_t; значения WS=7, EDGE=1, CORE=99 совпадают с C# enum.
Реализован undirected graph с add/remove/connect/disconnect и достижимостью через обход O(V+E).
Обход итеративный, циклы/изолированные/удалённые вершины покрыты тестами.

Unity сейчас не загружает библиотеку. Простой трехустройственный control circuit в C# проверяет
питание шлюза, это не альтернативная графовая библиотека. В будущем simulation adapter передаст
изменения topology/power нативному ядру. Графовые алгоритмы остаются C++.

Следующий контракт: opaque graph handle + C ABI functions для create/destroy/update/reachable,
целочисленные статусы ошибок, отсутствие C++ exceptions через ABI; owned lifetime у адаптера.
Desktop — platform plugin/PInvoke. Web — Emscripten static library/`__Internal`, отдельные артефакты
Unity Web toolchain. Desktop binary нельзя использовать в Web. Web/native adapters пока не созданы.

Проверка:
```
cmake -S native/hostfall-core -B .artifacts/native-debug -DCMAKE_BUILD_TYPE=Debug
cmake --build .artifacts/native-debug
ctest --test-dir .artifacts/native-debug --output-on-failure
```
Не заменять алгоритмы C#-портом и не блокировать первый playable slice отсутствующим ABI.
