PROGRAM main
  VAR
    ProductionTimer : TON;
    ResetTimer : BOOL := FALSE;
  END_VAR
  VAR
    OrderId AT %QW0 : UINT := 0;
    RequestedQuantity AT %QW1 : UINT := 0;
    ProductionStatus AT %QW2 : UINT := 0;
    ProducedQuantity AT %QW3 : UINT := 0;
    FaultCode AT %QW4 : UINT := 0;
    StartProduction AT %QX0.0 : BOOL := FALSE;
    ResetProduction AT %QX0.1 : BOOL := FALSE;
    ProductionComplete AT %QX0.2 : BOOL := FALSE;
    ProductionFault AT %QX0.3 : BOOL := FALSE;
    TotalProducedQuantity AT %QW5 : UINT := 0;
  END_VAR

  (* Återställer hela produktionsprocessen *)
  IF ResetProduction then
  ProductionStatus := 0;
  ProducedQuantity := 0;
  FaultCode := 0;
  ProductionComplete := FALSE;
  ProductionFault := FALSE;
  ResetTimer := FALSE;

  ProductionTimer(
      IN := False,
      PT := T#1s
  );

  else
          (* Starta endast när PLC:n är i Idle *)
      if StartProduction AND (ProductionStatus = 0) then
      (* Validera orderdata innan produktionen startar *)
      if OrderID = 0 then
      ProductionStatus := 3;
      FaultCode := 1;
      ProductionFault := TRUE;

  elsif RequestedQuantity = 0 then
  ProductionStatus := 3;
  FaultCode := 2;
  ProductionFault := TRUE;
  ELSIF RequestedQuantity > 20 THEN
      (* Simulerat produktionsfel - för stor order *)
      ProductionStatus := 3;
      FaultCode := 3;
      ProductionFault := TRUE;

          ELSE
              ProductionStatus := 1;
              ProducedQuantity := 0;
              FaultCode := 0;
              ProductionComplete := FALSE;
              ProductionFault := FALSE;
              ResetTimer := FALSE;
  end_if;
      
  end_if;
  (* Simulera att en produkt tillverkas varje sekund *)
      IF ProductionStatus = 1 THEN

          IF ResetTimer THEN
              ProductionTimer(
                  IN := FALSE,
                  PT := T#1s
              );

              ResetTimer := FALSE;

          ELSE
              ProductionTimer(
                  IN := TRUE,
                  PT := T#1s
              );

              IF ProductionTimer.Q THEN
                  ProducedQuantity := ProducedQuantity + 1;
                  TotalProducedQuantity := TotalProducedQuantity + 1;
                  ResetTimer := TRUE;

                  (* Markera ordern som färdig *)
                  IF ProducedQuantity >= RequestedQuantity THEN
                      ProductionStatus := 2;
                      ProductionComplete := TRUE;
                  END_IF;
              END_IF;
          END_IF;

      ELSE
          ProductionTimer(
              IN := FALSE,
              PT := T#1s
          );

          ResetTimer := FALSE;
      END_IF;
  END_IF;
END_PROGRAM


CONFIGURATION Config0

  RESOURCE Res0 ON PLC
    TASK task0(INTERVAL := T#20ms,PRIORITY := 1);
    PROGRAM instance0 WITH task0 : main;
  END_RESOURCE
END_CONFIGURATION
