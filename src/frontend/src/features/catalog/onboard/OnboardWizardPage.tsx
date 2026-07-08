import { useTranslation } from 'react-i18next';
import { useOnboardWizard } from './useOnboardWizard';
import { OnboardWizardShell } from './OnboardWizardShell';
import { OnboardIdentityPreview } from './OnboardIdentityPreview';
import { ServiceIdentityForm } from './ServiceIdentityForm';
import { ServiceInterfaceForm } from './ServiceInterfaceForm';
import { OnboardReviewStep } from './OnboardReviewStep';
import { TypeModeTab } from '../../contracts/create/tabs/TypeModeTab';
import { DetailsTab } from '../../contracts/create/tabs/DetailsTab';

/** Página do wizard de onboarding de serviço (rota /services/onboard). */
export function OnboardWizardPage() {
  const { t } = useTranslation();
  const w = useOnboardWizard();

  return (
    <OnboardWizardShell
      title={t('onboard.title', 'Onboard a service')}
      steps={w.steps}
      activeStep={w.activeStep}
      preview={<OnboardIdentityPreview values={w.identity} />}
      canGoNext={w.canGoNext}
      isFirstStep={w.isFirstStep}
      isLastStep={w.isLastStep}
      canSkip={w.canSkip}
      pending={w.pending}
      onBack={w.onBack}
      onNext={w.onNext}
      onSkip={w.onSkip}
      onCancel={w.onCancel}
    >
      {w.error && (
        <div className="mb-4 rounded-lg border border-critical/30 bg-critical/10 px-4 py-3 text-sm text-critical">
          {w.error}
        </div>
      )}

      {w.activeStep === 'identity' && (
        <>
          <h2 className="text-base font-semibold text-heading mb-4">{t('onboard.identity.heading', 'Service identity & ownership')}</h2>
          <ServiceIdentityForm values={w.identity} errors={w.identityErrors} onChange={w.setIdentityField} />
        </>
      )}

      {w.activeStep === 'interface' && (
        <>
          <h2 className="text-base font-semibold text-heading mb-1">{t('onboard.interface.heading', 'Expose an interface (optional)')}</h2>
          <p className="text-xs text-muted mb-4">{t('onboard.interface.skipHint')}</p>
          <ServiceInterfaceForm values={w.interface} errors={w.interfaceErrors} onChange={w.setInterfaceField} />
        </>
      )}

      {w.activeStep === 'contract' && (
        <>
          <h2 className="text-base font-semibold text-heading mb-1">{t('onboard.contract.heading', 'Define a contract (optional)')}</h2>
          <p className="text-xs text-muted mb-4">{t('onboard.contract.skipHint')}</p>
          <TypeModeTab
            filteredContractTypes={w.contractForm.filteredContractTypes}
            selectedType={w.contractForm.selectedType}
            onSelectType={w.contractForm.selectType}
            selectedMode={w.contractForm.selectedMode}
            onSelectMode={w.contractForm.setSelectedMode}
          />
          {w.contractForm.selectedType && w.contractForm.selectedMode && (
            <div className="mt-5 pt-5 border-t border-edge">
              <DetailsTab form={w.contractForm} />
            </div>
          )}
        </>
      )}

      {w.activeStep === 'review' && (
        <OnboardReviewStep
          identity={w.identity}
          interfaceValues={w.savedInterface}
          contractSummary={
            w.contractForm.selectedType
              ? { title: w.contractForm.title, type: w.contractForm.selectedType }
              : null
          }
        />
      )}
    </OnboardWizardShell>
  );
}
