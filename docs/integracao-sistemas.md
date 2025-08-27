# Problema: Dificuldade para Integração de Sistemas

## Descrição do Problema

Em arquiteturas tradicionais, adicionar novos sistemas ou funcionalidades requer modificações extensas no código existente, criando um efeito cascata de mudanças, deployments coordenados e risco de introduzir bugs em sistemas estáveis.

## Cenário: Adicionando Detecção de Fraude ao E-commerce

### Arquitetura Tradicional - Modificações Necessárias

```mermaid
sequenceDiagram
    participant Client as Cliente
    participant OS as OrderService (V1)
    participant PS as PaymentService
    participant IS as InventoryService
    participant ES as EmailService
    participant FS as FraudService (NOVO)

    Note over OS: Para adicionar detecção de fraude,<br/>é necessário MODIFICAR OrderService

    Client->>OS: Criar Pedido
    
    OS->>PS: Processar Pagamento
    PS-->>OS: Pagamento OK
    
    Note over OS: PRECISA MODIFICAR CÓDIGO<br/>para chamar FraudService
    OS->>FS: Verificar Fraude
    FS-->>OS: Sem Fraude Detectada
    
    OS->>IS: Verificar Estoque
    IS-->>OS: Estoque OK
    
    OS->>ES: Enviar Email
    ES-->>OS: Email Enviado
    
    OS-->>Client: Pedido Criado
    
    Note over Client,FS: PROBLEMA: Cada novo sistema<br/>requer modificação do código existente<br/>DEPLOY de todos os serviços afetados
```

#### Problemas Identificados:
- **Modificação de Código Existente**: OrderService precisa ser alterado
- **Deployments Coordenados**: Múltiplos serviços precisam ser atualizados
- **Risco de Regressão**: Mudanças podem introduzir bugs em funcionalidades estáveis
- **Time to Market Lento**: Cada nova funcionalidade requer refatoração
- **Testes Complexos**: Necessário re-testar toda a cadeia

### Arquitetura com EDA - Integração Transparente

```mermaid
sequenceDiagram
    participant Client as Cliente
    participant OS as OrderService (Inalterado)
    participant EB as Event Bus
    participant PS as PaymentService
    participant IS as InventoryService
    participant ES as EmailService
    participant FS as FraudService (NOVO)
    participant AS as AnalyticsService (NOVO)

    Note over OS: OrderService permanece INALTERADO<br/>Novos sistemas apenas "escutam" eventos

    Client->>OS: Criar Pedido
    
    OS->>EB: Publicar "OrderCreated"
    OS-->>Client: Pedido Criado
    
    par Processamento com Novos Sistemas
        EB->>PS: Evento "OrderCreated"
        PS->>EB: Publicar "PaymentProcessed"
    and
        EB->>IS: Evento "OrderCreated"
        IS->>EB: Publicar "InventoryReserved"
    and
        EB->>ES: Evento "OrderCreated"
        ES->>ES: Enviar Email
    and
        EB->>FS: Evento "OrderCreated" NOVO
        FS->>FS: Analisar Fraude
        FS->>EB: Publicar "FraudCheckCompleted" (se necessário)
    and
        EB->>AS: Evento "OrderCreated" NOVO
        AS->>AS: Coletar Métricas
    end
    
    Note over Client,AS: SOLUÇÃO: Novos sistemas adicionados<br/>SEM modificar código existente<br/>Deploy independente
```
 
