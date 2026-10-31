# Modelo de dados — Feature 006 (Busca de campanhas)

[← Spec](spec.md)

## Documento `CampanhaIndexada`

| Campo | Tipo no índice | Origem (`CampanhaAlteradaEvent`) |
|---|---|---|
| `_id` | — | `CampanhaId` |
| `titulo` | `text` (`pt_br`) + `titulo.exato` (`keyword`, normalizer `lowercase`) | `Titulo` |
| `descricao` | `text` (`pt_br`) | `Descricao` |
| `status` | `keyword` | `Status` |
| `metaFinanceira` | `scaled_float` (fator 100) | `MetaFinanceira` |
| `dataFim` | `date` | `DataFim` |
| `versao` | `long` | `Versao` (também versão externa do documento) |

## Índice `campanhas-v1` (alias `campanhas`)

```json
{
  "settings": {
    "number_of_shards": 1,
    "number_of_replicas": 0,
    "analysis": {
      "analyzer": {
        "pt_br": { "tokenizer": "standard", "filter": ["lowercase", "asciifolding", "stop_pt", "stemmer_pt"] }
      },
      "filter": {
        "stop_pt": { "type": "stop", "stopwords": "_brazilian_" },
        "stemmer_pt": { "type": "stemmer", "language": "brazilian" }
      }
    }
  },
  "mappings": {
    "properties": {
      "titulo": { "type": "text", "analyzer": "pt_br", "fields": { "exato": { "type": "keyword", "normalizer": "lowercase" } } },
      "descricao": { "type": "text", "analyzer": "pt_br" },
      "status": { "type": "keyword" },
      "metaFinanceira": { "type": "scaled_float", "scaling_factor": 100 },
      "dataFim": { "type": "date" },
      "versao": { "type": "long" }
    }
  }
}
```

## Consulta

```json
{
  "query": {
    "bool": {
      "must": {
        "multi_match": { "query": "<q>", "fields": ["titulo^3", "descricao"], "fuzziness": "AUTO", "prefix_length": 1 }
      },
      "should": [ { "match_phrase": { "titulo": { "query": "<q>", "boost": 5 } } } ],
      "filter": [ { "term": { "status": "Ativa" } } ]
    }
  },
  "highlight": { "fields": { "descricao": {} } },
  "sort": ["_score", { "dataFim": "asc" }],
  "from": "<(page-1)*pageSize>",
  "size": "<pageSize>"
}
```

O texto de `q` vai sempre como valor do `multi_match` (nunca como `query_string`), o que impede injeção de sintaxe.
