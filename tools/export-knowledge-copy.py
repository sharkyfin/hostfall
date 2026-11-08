# -*- coding: utf-8 -*-
"""Export exactly the authored copy used by Unity for editorial reading."""
import json
from pathlib import Path
root=Path(__file__).resolve().parents[1]
data=json.loads((root/'apps/unity/Assets/_HOSTFALL/Content/Resources/FieldContent.json').read_text(encoding='utf-8'))
lines=['# Каталог знаний HOSTFALL','','Редакционная выгрузка содержимого Unity.','']
for article in data['articles']:
    lines += ['## '+article['title']+' · '+article['id'],'',article['summary'],'',article['body'],'']
    for example in article['examples']:
        lines += ['Пример ('+example['language']+'):', '```',example['code'],'```','Результат: '+example['expected'],'']
    if article.get('mistakes'):lines += ['Частая ошибка: '+article['mistakes'],'']
(root/'docs/knowledge/editorial/CATALOG.md').write_text('\n'.join(lines),encoding='utf-8')
print('Exported',len(data['articles']),'articles')
