# language: pt
Funcionalidade: Recibo de doação
  Como doador
  Quero receber um recibo da doação
  Para ter um comprovante

  Cenário: Recibo após processamento
    Quando uma doação de 150.00 é processada
    Então a função registra "ReciboEmitido" com o doacaoId e o valor 150.00
    E o registro não contém nome, e-mail nem CPF

  Cenário: Evento repetido não gera recibo duplicado
    Dado um recibo já emitido para a doação "D1"
    Quando o evento da doação "D1" é entregue de novo
    Então nenhum novo "ReciboEmitido" é registrado para "D1"
