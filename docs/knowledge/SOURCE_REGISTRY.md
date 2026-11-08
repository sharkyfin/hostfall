# Source registry — 2026-11-06

Собственный пересказ; длинные цитаты не копируются. Дата просмотра источника
не означает запуск всех команд. Неполученные POSIX/GNU страницы не служат подтверждением.

## Язык программирования

Python 3; пример проверяется Python 3.9

- [docs.python.org](https://docs.python.org/3/tutorial/introduction.html)

Проверка: Выполнено: Python 3.9.6; результат проверен 2026-11-06.

## Алгоритм

Общее понятие; JavaScript ECMAScript

- [xlinux.nist.gov](https://xlinux.nist.gov/dads/HTML/algorithm.html)
- [developer.mozilla.org](https://developer.mozilla.org/en-US/docs/Web/JavaScript/Reference/Global_Objects/Array/filter)

Проверка: Выполнено: v24.21.0; результат проверен 2026-11-06.

## Терминал и оболочка

HOSTFALL nlsh subset 1; терминальные понятия

- [learn.microsoft.com](https://learn.microsoft.com/en-us/windows/console/consoles)

Проверка: Выполнено в виртуальной модели: FieldContentTests, 2026-11-06. Это не запуск Linux..

## Процесс и служба

Unix signals; systemd documented subset; HOSTFALL 1

- [man7.org](https://man7.org/linux/man-pages/man1/ps.1.html)
- [man7.org](https://man7.org/linux/man-pages/man1/kill.1.html)
- [man7.org](https://man7.org/linux/man-pages/man7/pthreads.7.html)
- [www.freedesktop.org](https://www.freedesktop.org/software/systemd/man/latest/systemctl.html)

Проверка: Выполнено в виртуальной модели: FieldContentTests, 2026-11-06. Это не запуск Linux..

## Файл и файловая система

Unix path concepts; HOSTFALL virtual FS 1

- [man7.org](https://man7.org/linux/man-pages/man7/path_resolution.7.html)

Проверка: Выполнено в виртуальной модели: FieldContentTests, 2026-11-06. Это не запуск Linux..

## IP-адрес

IPv4 RFC 791: формат; без устаревшей классовой маршрутизации

- [www.rfc-editor.org](https://www.rfc-editor.org/rfc/rfc791)

Проверка: Выполнено в виртуальной модели: FieldContentTests, 2026-11-06. Это не запуск Linux..

## Сервер

Клиент-серверные понятия; MDN 2026-11-06

- [developer.mozilla.org](https://developer.mozilla.org/en-US/docs/Learn_web_development/Howto/Web_mechanics/What_is_a_web_server)

Проверка: Выполнено в виртуальной модели: FieldContentTests, 2026-11-06. Это не запуск Linux..

## HTTP

HTTP concepts; пример HTTP/1.1

- [developer.mozilla.org](https://developer.mozilla.org/en-US/docs/Web/HTTP/Guides/Overview)
- [developer.mozilla.org](https://developer.mozilla.org/en-US/docs/Web/HTTP/Guides/Evolution_of_HTTP)

Проверка: Не выполнялся; пример для чтения.

## HTML

HTML Living Standard: базовые элементы, MDN

- [developer.mozilla.org](https://developer.mozilla.org/en-US/docs/Web/HTML)

Проверка: Не выполнялся; пример для чтения.

## CSS

CSS: модульная спецификация; базовые селекторы

- [developer.mozilla.org](https://developer.mozilla.org/en-US/docs/Web/CSS)

Проверка: Не выполнялся; пример для чтения.

## Контейнер

Linux container concepts; Docker CLI docs, без фиксации runtime

- [docs.docker.com](https://docs.docker.com/get-started/docker-concepts/the-basics/what-is-a-container/)

Проверка: Не выполнялся; пример для чтения.

## Kubernetes

Kubernetes stable concepts; версия лаборатории ещё не выбрана

- [kubernetes.io](https://kubernetes.io/docs/concepts/overview/)
- [kubernetes.io](https://kubernetes.io/docs/concepts/workloads/pods/)
- [kubernetes.io](https://kubernetes.io/docs/concepts/workloads/controllers/deployment/)
- [kubernetes.io](https://kubernetes.io/docs/concepts/services-networking/service/)

Проверка: Не выполнялся; пример для чтения.

Python/JS запускает tools/verify-field-examples.py в доверенной среде разработки,
nlsh — FieldContentTests. Это не sandbox пользовательских программ. HTML/CSS
требуют отдельного браузерного просмотра, Docker/kubectl/HTTP не запускались.
