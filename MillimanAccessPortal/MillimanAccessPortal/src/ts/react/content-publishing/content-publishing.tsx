import '../../../scss/react/content-publishing/content-publishing.scss';

import '../../../images/icons/add.svg';
import '../../../images/icons/expand-frame.svg';
import '../../../images/icons/user.svg';

import * as React from 'react';
import * as Modal from 'react-modal';
import { connect } from 'react-redux';
import ReduxToastr from 'react-redux-toastr';

import { convertMarkdownToHTML } from '../../convert-markdown';
import { setUnloadAlert } from '../../unload-alerts';
import {
  ContentTypeEnum, isPublicationActive, PublicationStatus, PublishRequest,
} from '../../view-models/content-publishing';
import { ContentCard } from '../authorized-content/content-card';
import {
  Client, ClientWithStats, ContentAssociatedFileType, ContentItemPublicationDetail,
  ContentType, RootContentItem, RootContentItemWithPublication,
} from '../models';
import { ActionIcon } from '../shared-components/action-icon';
import { BrowserSupportBanner } from '../shared-components/browser-support-banner';
import { ButtonSpinner } from '../shared-components/button-spinner';
import { CardPanel } from '../shared-components/card-panel/card-panel';
import {
    PanelSectionToolbar, PanelSectionToolbarButtons,
} from '../shared-components/card-panel/panel-sections';
import { Card } from '../shared-components/card/card';
import CardButton from '../shared-components/card/card-button';
import {
   CardSectionButtons, CardSectionMain, CardSectionStats, CardText,
} from '../shared-components/card/card-sections';
import { CardStat } from '../shared-components/card/card-stat';
import { ContentContainer, contentTypeMap } from '../shared-components/content-container';
import {
  ContentPanel, ContentPanelSectionContent,
} from '../shared-components/content-panel/content-panel';
import { Filter } from '../shared-components/filter';
import { Checkbox } from '../shared-components/form/checkbox';
import { FileUploadInput } from '../shared-components/form/file-upload-input';
import {
  ContentPanelForm, FormFlexContainer, FormSection, FormSectionRow,
} from '../shared-components/form/form-elements';
import { Input, MultiAddInput, TextAreaInput } from '../shared-components/form/input';
import { DropDown } from '../shared-components/form/select';
import { Toggle } from '../shared-components/form/toggle';
import { NavBar } from '../shared-components/navbar';
import { Dict } from '../shared-components/redux/store';
import { TabRow } from '../shared-components/tab-row';
import { GoLiveSection } from './go-live-section';
import { HierarchyDiffs } from './hierarchy-diffs';
import * as PublishingActionCreators from './redux/action-creators';
import {
  activeSelectedClient, activeSelectedItem, availableAssociatedContentTypes,
  availableContentTypes, canDownloadCurrentContentItem, clientEntities, contentItemForPublication,
  contentItemToBeCanceled, contentItemToBeDeleted, filesForPublishing, formChangesPending, goLiveApproveButtonIsActive,
  itemEntities, selectedItem, submitButtonIsActive, uploadChangesPending,
} from './redux/selectors';
import {
  GoLiveSummaryData, PublishingFormData, PublishingState, PublishingStateCardAttributes,
  PublishingStateFilters, PublishingStateModals, PublishingStatePending, PublishingStateSelected,
} from './redux/store';
import { SelectionGroupDetails } from './selection-group-detail';

type ClientEntity = (ClientWithStats & { indent: 1 | 2 }) | 'divider';
interface RootContentItemEntity extends RootContentItemWithPublication {
  contentTypeName: string;
}

interface ContentPublishingProps {
  clients: ClientEntity[];
  items: RootContentItemEntity[];
  contentTypes: Dict<ContentType>;
  contentAssociatedFileTypes: Dict<ContentAssociatedFileType>;
  contentTypesList: Array<{ selectionValue: string | number, selectionLabel: string }>;
  associatedContentTypesList: Array<{ selectionValue: string | number, selectionLabel: string }>;
  formData: PublishingFormData;
  goLiveSummary: GoLiveSummaryData;
  selected: PublishingStateSelected;
  cardAttributes: PublishingStateCardAttributes;
  pending: PublishingStatePending;
  filters: PublishingStateFilters;
  modals: PublishingStateModals;

  selectedItem: RootContentItem;
  activeSelectedClient: Client;
  activeSelectedItem: RootContentItem;
  filesForPublishing: PublishRequest;
  contentItemToBeDeleted: RootContentItem;
  contentItemToBeCanceled: RootContentItem;
  formCanSubmit: boolean;
  formChangesPending: boolean;
  goLiveApproveButtonIsActive: boolean;
  uploadChangesPending: boolean;
  canDownloadCurrentContentItem: boolean;
  contentItemForPublication: ContentItemPublicationDetail;
}

class ContentPublishing extends React.Component<ContentPublishingProps & typeof PublishingActionCreators> {
  private readonly currentView: string = document
    .getElementsByTagName('body')[0].getAttribute('data-nav-location');

  public componentDidMount() {
    this.props.fetchGlobalData({});
    this.props.fetchClients({});
    this.props.scheduleStatusRefresh({ delay: 0 });
    this.props.scheduleSessionCheck({ delay: 0 });
    setUnloadAlert(() => this.props.formChangesPending || this.props.uploadChangesPending);
  }

  public render() {
    const { modals, selected, formData, goLiveSummary, pending } = this.props;
    const { pendingFormData, originalFormData } = this.props.formData;
    return (
      <>
        <ReduxToastr
          timeOut={5000}
          newestOnTop={false}
          position="bottom-center"
          transitionIn="fadeIn"
          transitionOut="fadeOut"
        />
        <NavBar currentView={this.currentView} />
        <BrowserSupportBanner />
        {this.renderClientPanel()}
        {selected.client && this.renderItemPanel()}
        {(goLiveSummary.rootContentItemId)
          ? this.renderGoLiveSummary()
          : (selected.item
            && pendingFormData.clientId) ? this.renderContentItemForm() : null
        }
        <Modal
          isOpen={modals.contentItemDeletion.isOpen}
          onRequestClose={() => this.props.closeDeleteContentItemModal({})}
          ariaHideApp={false}
          className="modal"
          overlayClassName="modal-overlay"
          closeTimeoutMS={100}
        >
          <h3 className="title red">Delete Content Item</h3>
          <span className="modal-text">
            Delete <strong>{
              (this.props.contentItemToBeDeleted !== null)
                ? this.props.contentItemToBeDeleted.name
                : ''}</strong>?
          </span>
          <div className="button-container">
            <button
              className="link-button"
              type="button"
              onClick={() => this.props.closeDeleteContentItemModal({})}
            >
              Cancel
            </button>
            <button
              className="red-button"
              onClick={() => {
                // Add a slight pause to make it obvious that you've switched modals
                setTimeout(() => this.props.openDeleteConfirmationModal({}), 400);
              }}
            >
              Delete
              {this.props.pending.data.contentItemDeletion
                ? <ButtonSpinner version="circle" />
                : null
              }
            </button>
          </div>
        </Modal>
        <Modal
          isOpen={modals.contentItemDeleteConfirmation.isOpen}
          onRequestClose={() => this.props.closeDeleteConfirmationModal({})}
          ariaHideApp={false}
          className="modal"
          overlayClassName="modal-overlay"
          closeTimeoutMS={100}
        >
          <h3 className="title red">Confirm Deletion of Content Item</h3>
          <span className="modal-text">
            Delete <strong>{
              (this.props.contentItemToBeDeleted !== null)
                ? this.props.contentItemToBeDeleted.name
                : ''}</strong>?
            <br />
            <br />
            <strong>THIS ACTION CANNOT BE UNDONE.</strong>
          </span>
          <div className="button-container">
            <button
              className="link-button"
              type="button"
              onClick={() => this.props.closeDeleteConfirmationModal({})}
            >
              Cancel
            </button>
            <button
              className="red-button"
              onClick={() => {
                if (!this.props.pending.data.contentItemDeletion) {
                  this.props.deleteContentItem(this.props.pending.contentItemToDelete);
                }
              }}
            >
              Confirm Deletion
              {this.props.pending.data.contentItemDeletion
                ? <ButtonSpinner version="circle" />
                : null
              }
            </button>
          </div>
        </Modal>
        <Modal
          isOpen={modals.goLiveRejection.isOpen}
          onRequestClose={() => this.props.closeGoLiveRejectionModal({})}
          ariaHideApp={false}
          className="modal"
          overlayClassName="modal-overlay"
          closeTimeoutMS={100}
        >
          <h3 className="title red">Reject Publication</h3>
          <span className="modal-text">
            Reject Publication of <strong>{
              this.props.goLiveSummary.goLiveSummary && this.props.goLiveSummary.goLiveSummary.rootContentName
            }</strong>?
          </span>
          <div className="button-container">
            <button
              className="link-button"
              type="button"
              onClick={() => this.props.closeGoLiveRejectionModal({})}
            >
              Cancel
            </button>
            <button
              className="red-button"
              onClick={() => {
                if (!this.props.pending.data.goLiveRejection) {
                  this.props.rejectGoLiveSummary({
                    rootContentItemId: this.props.goLiveSummary.rootContentItemId,
                    publicationRequestId: this.props.goLiveSummary.goLiveSummary.publicationRequestId,
                    validationSummaryId: this.props.goLiveSummary.goLiveSummary.validationSummaryId,
                  });
                }
              }}
            >
              Reject
              {this.props.pending.data.goLiveRejection
                ? <ButtonSpinner version="circle" />
                : null
              }
            </button>
          </div>
        </Modal>
        <Modal
          isOpen={modals.formModified.isOpen}
          onRequestClose={() => this.props.closeModifiedFormModal({})}
          ariaHideApp={false}
          className="modal"
          overlayClassName="modal-overlay"
          closeTimeoutMS={100}
        >
          <h3 className="title red">Discard Changes</h3>
          <span className="modal-text">Would you like to discard unsaved changes?</span>
          <div className="button-container">
            <button
              className="link-button"
              type="button"
              onClick={() => this.props.closeModifiedFormModal({})}
            >
              Continue Editing
            </button>
            <button
              className="red-button"
              onClick={() => {
                const { entityToSelect, entityType } = pending.afterFormModal;
                this.props.resetContentItemForm({});
                switch (entityType) {
                  case 'Select Client':
                    if (selected.client !== entityToSelect) {
                      this.props.fetchItems({ clientId: entityToSelect });
                    }
                    this.props.selectClient({ id: entityToSelect });
                    break;
                  case 'Select Content Item':
                    if (selected.item !== entityToSelect && entityToSelect !== null) {
                      this.props.fetchContentItemDetail({ rootContentItemId: entityToSelect });
                    }
                    this.props.selectItem({ id: entityToSelect });
                    this.props.setContentItemFormState({ formState: 'read' });
                    break;
                  case 'Delete Content Item':
                    // Add a slight pause to make it obvious that you've switched modals
                    setTimeout(() => this.props.openDeleteContentItemModal({ id: entityToSelect }), 400);
                    break;
                  case 'Update Content Item':
                    this.props.fetchContentItemDetail({ rootContentItemId: entityToSelect });
                    this.props.selectItem({ id: entityToSelect });
                    this.props.setContentItemFormState({ formState: 'write' });
                    break;
                  case 'New Content Item':
                    this.props.setFormForNewContentItem({ clientId: selected.client });
                    break;
                  case 'Undo Changes':
                    // This action is triggered for every outcome
                    break;
                  case 'Go Live Summary':
                    this.props.fetchGoLiveSummary({ rootContentItemId: entityToSelect });
                    break;
                }
              }}
            >
              Discard
            </button>
          </div>
        </Modal>
        <Modal
          isOpen={modals.cancelPublication.isOpen}
          onRequestClose={() => this.props.closeCancelPublicationModal({})}
          ariaHideApp={false}
          className="modal"
          overlayClassName="modal-overlay"
          closeTimeoutMS={100}
        >
          <h3 className="title red">Cancel Publication Request</h3>
          <span className="modal-text">
            Would you like to cancel the publication request for <strong>{
              (this.props.contentItemToBeCanceled !== null)
                ? this.props.contentItemToBeCanceled.name
                : ''}</strong>?
          </span>
          <div className="button-container">
            <button
              className="link-button"
              type="button"
              onClick={() => this.props.closeCancelPublicationModal({})}
            >
              Continue
            </button>
            <button
              className="red-button"
              onClick={() => {
                if (!this.props.pending.data.cancelPublication) {
                  this.props.cancelPublicationRequest(this.props.pending.publicationToCancel);
                }
              }}
            >
              Cancel Publication
              {this.props.pending.data.cancelPublication
                ? <ButtonSpinner version="circle" />
                : null
              }
            </button>
          </div>
        </Modal>
      </>
    );
  }

  private renderClientPanel() {
    const { clients, selected, filters, pending, cardAttributes } = this.props;
    return (
      <CardPanel
        entities={clients}
        loading={pending.data.clients}
        renderEntity={(entity, key) => {
          if (entity === 'divider') {
            return <div className="hr" key={key} />;
          }
          const card = cardAttributes.client[entity.id];
          return (
            <Card
              key={key}
              selected={selected.client === entity.id}
              disabled={card.disabled}
              onSelect={() => {
                if (this.props.formChangesPending || this.props.uploadChangesPending) {
                  this.props.openModifiedFormModal({
                    afterFormModal:
                    {
                      entityToSelect: entity.id,
                      entityType: 'Select Client',
                    },
                  });
                } else {
                  if (selected.client !== entity.id) {
                    this.props.fetchItems({ clientId: entity.id });
                  }
                  this.props.selectClient({ id: entity.id });
                }
              }}
              indentation={entity.indent}
            >
              <CardSectionMain>
                <CardText text={entity.name} subtext={entity.code} />
                <CardSectionStats>
                  <CardStat
                    name={'Content-assigned users'}
                    value={entity.userCount}
                    icon={'user'}
                  />
                  <CardStat
                    name={'Content items'}
                    value={entity.contentItemCount}
                    icon={'reports'}
                  />
                </CardSectionStats>
              </CardSectionMain>
            </Card>
          );
        }}
      >
        <h3 className="admin-panel-header">Clients</h3>
        <PanelSectionToolbar>
          <Filter
            placeholderText={'Filter clients...'}
            setFilterText={(text) => this.props.setFilterTextClient({ text })}
            filterText={filters.client.text}
          />
          <PanelSectionToolbarButtons>
            <div id="icons" />
          </PanelSectionToolbarButtons>
        </PanelSectionToolbar>
      </CardPanel>
    );
  }

  private renderItemPanel() {
    const {
      activeSelectedClient: activeClient, items, selected, filters, pending, modals,
    } = this.props;
    const createNewContentItemIcon = (
      <ActionIcon
        label="New Content Item"
        icon="add"
        action={() => {
          if (this.props.formChangesPending || this.props.uploadChangesPending) {
            this.props.openModifiedFormModal({
              afterFormModal:
              {
                entityToSelect: null,
                entityType: 'New Content Item',
              },
            });
          } else {
            this.props.setFormForNewContentItem({ clientId: selected.client });
          }
        }}
      />
    );
    const cardButtons = (entityId: string, entityPublicationStatus: PublicationStatus) => {
      switch (entityPublicationStatus) {
        case PublicationStatus.Processed:
          return (
            <>
              <CardButton
                color={'green'}
                tooltip={'Review for Approval'}
                onClick={() => {
                  if (this.props.formChangesPending || this.props.uploadChangesPending) {
                    this.props.openModifiedFormModal({
                      afterFormModal:
                      {
                        entityToSelect: entityId,
                        entityType: 'Go Live Summary',
                      },
                    });
                  } else {
                    this.props.fetchGoLiveSummary({ rootContentItemId: entityId });
                  }
                }}
                icon={'checkmark'}
              />
            </>
          );
        case PublicationStatus.Validating:
        case PublicationStatus.Queued:
        case PublicationStatus.Processing:
        case PublicationStatus.PostProcessReady:
        case PublicationStatus.Error:
          return (
            <>
              <CardButton
                color={'red'}
                tooltip={'Cancel Publication'}
                onClick={() => this.props.openCancelPublicationModal({ id: entityId })}
                icon={'cancel'}
              />
            </>
          );
        case PublicationStatus.PostProcessing:
          return null;
        default:
          return (
            <>
              <CardButton
                color={'red'}
                tooltip={'Delete Content Item'}
                onClick={() => {
                  if (this.props.formChangesPending || this.props.uploadChangesPending) {
                    this.props.openModifiedFormModal({
                      afterFormModal:
                      {
                        entityToSelect: entityId,
                        entityType: 'Delete Content Item',
                      },
                    });
                  } else {
                    this.props.openDeleteContentItemModal({ id: entityId });
                  }
                }}
                icon={'delete'}
              />
              <CardButton
                color={'blue'}
                tooltip={'Update Content Item'}
                onClick={() => {
                  if (this.props.formChangesPending || this.props.uploadChangesPending) {
                    this.props.openModifiedFormModal({
                      afterFormModal:
                      {
                        entityToSelect: entityId,
                        entityType: 'Update Content Item',
                      },
                    });
                  } else {
                    if (selected.item !== entityId) {
                      this.props.fetchContentItemDetail({ rootContentItemId: entityId });
                      this.props.selectItem({ id: entityId });
                    }
                    this.props.setContentItemFormState({ formState: 'write' });
                  }
                }}
                icon={'upload'}
              />
            </>
          );
      }
    };
    return activeClient && (
      <CardPanel
        entities={items}
        loading={pending.data.items}
        renderEntity={(entity, key) => {
          return (
            <Card
              key={key}
              selected={selected.item === entity.id}
              onSelect={() => {
                if (this.props.formChangesPending || this.props.uploadChangesPending) {
                  this.props.openModifiedFormModal({
                    afterFormModal:
                    {
                      entityToSelect: entity.id,
                      entityType: 'Select Content Item',
                    },
                  });
                } else {
                  if (selected.item !== entity.id) {
                    this.props.fetchContentItemDetail({ rootContentItemId: entity.id });
                  }
                  this.props.selectItem({ id: entity.id });
                  this.props.setContentItemFormState({ formState: 'read' });
                }
              }}
              suspended={entity.isSuspended}
              status={entity.status}
            >
              <CardSectionMain>
                <CardText
                  text={entity.name}
                  textSuffix={entity.isSuspended ? '[Suspended]' : ''}
                  subtext={entity.contentTypeName}
                />
                <CardSectionStats>
                  <CardStat
                    name={'Assigned users'}
                    value={entity.assignedUserCount}
                    icon={'user'}
                  />
                  <CardStat
                    name={'Selection groups'}
                    value={entity.selectionGroupCount}
                    icon={'group'}
                  />
                </CardSectionStats>
                <CardSectionButtons>
                  {cardButtons(entity.id, entity.status.requestStatus)}
                </CardSectionButtons>
              </CardSectionMain>
            </Card>
            );
        }}
        renderNewEntityButton={() => (
          <div
            className="card-container action-card-container"
            onClick={() => {
              if (this.props.formChangesPending || this.props.uploadChangesPending) {
                this.props.openModifiedFormModal({
                  afterFormModal:
                  {
                    entityToSelect: null,
                    entityType: 'New Content Item',
                  },
                });
              } else {
                this.props.setFormForNewContentItem({ clientId: selected.client });
              }
            }}
          >
            <div className="admin-panel-content">
              <div
                className={
                  `
                    card-body-container card-100 action-card
                    ${this.props.selected.item === 'NEW CONTENT ITEM' ? 'selected' : ''}
                  `
                }
              >
                <h2 className="card-body-primary-text">
                  <svg className="action-card-icon">
                    <use href="#add" />
                  </svg>
                  <span>CREATE CONTENT ITEM</span>
                </h2>
              </div>
            </div>
          </div>
        )}
      >
        <h3 className="admin-panel-header">Content Items</h3>
        <PanelSectionToolbar>
          <Filter
            placeholderText={'Filter content items...'}
            setFilterText={(text) => this.props.setFilterTextItem({ text })}
            filterText={filters.item.text}
          />
          <PanelSectionToolbarButtons>
            {createNewContentItemIcon}
          </PanelSectionToolbarButtons>
        </PanelSectionToolbar>
      </CardPanel>
    );
  }

  private renderContentItemForm() {
    const { contentTypes, formData, items, pending } = this.props;
    const { formErrors, pendingFormData, originalFormData, formState, uploads } = formData;
    const editFormButton = (
      <>
        {
          this.props.canDownloadCurrentContentItem &&
          <a
            href={`./ContentPublishing/DownloadPowerBiContentItem?contentItemId=${pendingFormData.id}`}
            download={true}
          >
            <ActionIcon
              label="Download Editable Power BI Content Item"
              icon="download"
              action={() => null}
            />
          </a>
        }
        <ActionIcon
          label="Update Content Item"
          icon="upload"
          action={() => { this.props.setContentItemFormState({ formState: 'write' }); }}
        />
      </>
    );
    const selectedItemStatus = items.filter((x) => x.id === pendingFormData.id)[0];
    const closeFormButton = (
      <ActionIcon
        label="Close Content Item"
        icon="cancel"
        action={() => {
          if (this.props.formChangesPending || this.props.uploadChangesPending) {
            this.props.openModifiedFormModal({
              afterFormModal:
              {
                entityToSelect: null,
                entityType: 'Select Content Item',
              },
            });
          } else {
            this.props.selectItem({ id: null });
          }
        }}
      />
    );
    return (
      <ContentPanel loading={pending.data.contentItemDetail}>
        <h3 className="admin-panel-header">Content Item</h3>
        <PanelSectionToolbar>
          <PanelSectionToolbarButtons>
            {formState === 'read'
              && pendingFormData.id
              && selectedItemStatus
              && !isPublicationActive(selectedItemStatus.status.requestStatus)
              && editFormButton
            }
            {closeFormButton}
          </PanelSectionToolbarButtons>
        </PanelSectionToolbar>
        <ContentPanelSectionContent>
          <ContentPanelForm
            readOnly={formState === 'read'}
          >
            <FormSection title="Content Item Information">
              <FormSectionRow>
                <FormFlexContainer flexPhone={12}>
                  <Input
                    autoFocus={true}
                    error={formErrors.contentName}
                    label="Content Name"
                    placeholderText="Content Name *"
                    name="contentName"
                    onBlur={() => false}
                    onChange={({ currentTarget: target }: React.FormEvent<HTMLInputElement>) => {
                      this.props.setPublishingFormTextInputValue({
                        inputName: 'contentName',
                        value: target.value,
                      });
                    }}
                    type="text"
                    value={pendingFormData.contentName}
                    readOnly={formState === 'read'}
                  />
                </FormFlexContainer>
                <FormFlexContainer flexPhone={12} flexTablet={4}>
                  <DropDown
                    error={formData.formErrors.contentTypeId}
                    label="Content Type"
                    name="contentType"
                    onChange={({ currentTarget: target }: React.FormEvent<HTMLSelectElement>) => {
                      this.props.setPublishingFormTextInputValue({
                        inputName: 'contentTypeId',
                        value: target.value,
                      });
                    }}
                    placeholderText="Content Type *"
                    value={pendingFormData.contentTypeId}
                    values={this.props.contentTypesList}
                    readOnly={
                      formState === 'read'
                      || originalFormData.id.length > 0
                      || (pendingFormData.relatedFiles.MasterContent
                        && pendingFormData.relatedFiles.MasterContent.fileOriginalName.length > 0)
                    }
                  />
                </FormFlexContainer>
                <FormFlexContainer flexPhone={12} flexTablet={8}>
                  <FileUploadInput
                    fileExtensions={
                      contentTypes[pendingFormData.contentTypeId] !== undefined
                        ? contentTypes[pendingFormData.contentTypeId].fileExtensions
                        : []
                    }
                    label="File"
                    name="masterContent"
                    placeholderText="File *"
                    beginUpload={(uploadId, fileName) =>
                      this.props.beginFileUpload({ uploadId, fileName })}
                    cancelFileUpload={(uploadId) =>
                      this.props.cancelFileUpload({ uploadId })}
                    finalizeUpload={(uploadId, fileName, guid) =>
                      this.props.finalizeUpload({ uploadId, fileName, guid })}
                    setUploadError={(uploadId, errorMsg) =>
                      this.props.setUploadError({ uploadId, errorMsg })}
                    updateChecksumProgress={(uploadId, progress) =>
                      this.props.updateChecksumProgress({ uploadId, progress })}
                    updateUploadProgress={(uploadId, progress) =>
                      this.props.updateUploadProgress({ uploadId, progress })}
                    upload={uploads[pendingFormData.relatedFiles.MasterContent.uniqueUploadId]}
                    uploadId={pendingFormData.relatedFiles.MasterContent.uniqueUploadId}
                    fileUploadId={
                      (pendingFormData.relatedFiles.MasterContent.fileUploadId !== undefined)
                        ? pendingFormData.relatedFiles.MasterContent.fileUploadId
                        : null
                    }
                    value={pendingFormData.relatedFiles.MasterContent.fileOriginalName}
                    readOnly={formState === 'read' || pendingFormData.contentTypeId.length === 0}
                  />
                </FormFlexContainer>
              </FormSectionRow>
            </FormSection>
            {
              pendingFormData && pendingFormData.contentTypeId &&
              (
                contentTypes[pendingFormData.contentTypeId].displayName === 'QlikView' ||
                contentTypes[pendingFormData.contentTypeId].displayName === 'Power BI'
              ) &&
              <FormSection
                title={`${contentTypes[pendingFormData.contentTypeId].displayName} Specific Settings`}
              >
                {
                  contentTypes[pendingFormData.contentTypeId].displayName === 'QlikView' &&
                  <Checkbox
                    name="Reducible"
                    selected={pendingFormData.doesReduce}
                    onChange={(status) => this.props.setPublishingFormBooleanInputValue({
                      inputName: 'doesReduce',
                      value: status,
                    })}
                    readOnly={formState === 'read' || originalFormData.id.length > 0}
                    hoverText="This can only be changed on the initial publication of this content item"
                  />
                }
                {
                  contentTypes[pendingFormData.contentTypeId].displayName === 'Power BI' &&
                  <>
                    <h4>Document Settings</h4>
                    <Checkbox
                      name="Editable"
                      selected={pendingFormData.typeSpecificDetailObject.editableEnabled}
                      onChange={(status) => this.props.setPublishingFormBooleanInputValue({
                        inputName: 'editableEnabled',
                        value: status,
                      })}
                      readOnly={formState === 'read'}
                      description={' - this will give Content Access Admins the option to allow users to edit ' +
                                   ' the document in MAP, and upon save will update the content for all users'}
                    />
                    <Checkbox
                      name="Navigation Pane"
                      selected={pendingFormData.typeSpecificDetailObject.navigationPaneEnabled}
                      onChange={(status) => this.props.setPublishingFormBooleanInputValue({
                        inputName: 'navigationPaneEnabled',
                        value: status,
                      })}
                      readOnly={formState === 'read'}
                      description={' - this will allow users to navigate between the pages of the Power BI document'}
                    />
                    <Checkbox
                      name="Filter Pane"
                      selected={pendingFormData.typeSpecificDetailObject.filterPaneEnabled}
                      onChange={(status) => this.props.setPublishingFormBooleanInputValue({
                        inputName: 'filterPaneEnabled',
                        value: status,
                      })}
                      readOnly={formState === 'read'}
                      description={' - this will allow users to configure which filters to include and' +
                                   ' update existing filters'}
                    />
                    <Checkbox
                      name="Bookmark Pane"
                      selected={pendingFormData.typeSpecificDetailObject.bookmarksPaneEnabled}
                      onChange={(status) => this.props.setPublishingFormBooleanInputValue({
                        inputName: 'bookmarksPaneEnabled',
                        value: status,
                      })}
                      readOnly={formState === 'read'}
                      description={' - this will allow users to capture the current view of a report page' +
                                   ' including filters and the state of visuals'}
                    />
                    <h4>Security Settings</h4>
                    <Checkbox
                      name="Row Level Security"
                      selected={pendingFormData.doesReduce}
                      onChange={(status) => this.props.setPublishingFormBooleanInputValue({
                        inputName: 'doesReduce',
                        value: status,
                      })}
                      readOnly={formState === 'read' || originalFormData.id.length > 0}
                      description={' - this can be used to restrict data access for any given user.  The ability ' +
                                   'to restrict access will be accessible in the Content Access Admin tab'}
                    />
                    {
                      (pendingFormData.doesReduce || originalFormData.doesReduce) && (
                        formState === 'read' ?
                          <TextAreaInput
                            name="roleList"
                            label="Power BI Role Values"
                            value={pendingFormData.typeSpecificPublicationProperties ?
                              pendingFormData.typeSpecificPublicationProperties.roleList.join(', ') : null}
                            readOnly={true}
                            error={null}
                          /> :
                          <MultiAddInput
                            name="rolesList"
                            label="Power BI Role Names"
                            type="text"
                            list={pendingFormData.typeSpecificPublicationProperties ?
                              pendingFormData.typeSpecificPublicationProperties.roleList : []}
                            value={''}
                            addItem={(item: string, _: boolean, itemAlreadyExists: boolean) => {
                              if (itemAlreadyExists) {
                                toastr.warning('', 'That role already exists.');
                              } else {
                                this.props.appendPublishingFormTextArrayValue({
                                  inputName: 'roleList',
                                  value: item,
                                });
                              }
                            }}
                            removeItemCallback={(index: number) => {
                              this.props.setPublishingFormTextArrayValue({
                                inputName: 'roleList',
                                value:
                                  pendingFormData.typeSpecificPublicationProperties.roleList.slice(0, index)
                                  .concat(
                                    pendingFormData.typeSpecificPublicationProperties.roleList.slice(index + 1),
                                  ),
                              });
                            }}
                            error={null}
                          />
                      )
                    }
                  </>
                }
              </FormSection>
            }
            <FormSection title="Content Related Files">
              <FormSectionRow>
                <FormFlexContainer flexPhone={4}>
                  <FileUploadInput
                    fileExtensions={['jpg', 'jpeg', 'gif', 'png']}
                    label="Thumbnail"
                    name="thumbnail"
                    placeholderText="Thumbnail"
                    imageURL={originalFormData.thumbnailLink}
                    beginUpload={(uploadId, fileName) =>
                      this.props.beginFileUpload({ uploadId, fileName })}
                    cancelFileUpload={(uploadId) =>
                      this.props.cancelFileUpload({ uploadId })}
                    removeExistingFile={(uploadId) =>
                      this.props.removeExistingFile({ uploadId })}
                    finalizeUpload={(uploadId, fileName, guid) =>
                      this.props.finalizeUpload({ uploadId, fileName, guid })}
                    setUploadError={(uploadId, errorMsg) =>
                      this.props.setUploadError({ uploadId, errorMsg })}
                    updateChecksumProgress={(uploadId, progress) =>
                      this.props.updateChecksumProgress({ uploadId, progress })}
                    updateUploadProgress={(uploadId, progress) =>
                      this.props.updateUploadProgress({ uploadId, progress })}
                    upload={uploads[pendingFormData.relatedFiles.Thumbnail.uniqueUploadId]}
                    uploadId={pendingFormData.relatedFiles.Thumbnail.uniqueUploadId}
                    fileUploadId={
                      (pendingFormData.relatedFiles.Thumbnail.fileUploadId !== undefined)
                        ? pendingFormData.relatedFiles.Thumbnail.fileUploadId
                        : null
                    }
                    value={pendingFormData.relatedFiles.Thumbnail.fileOriginalName}
                    readOnly={formState === 'read'}
                  />
                </FormFlexContainer>
                <FormFlexContainer flexPhone={8}>
                  <FileUploadInput
                    fileExtensions={['pdf']}
                    label="User Guide"
                    name="userGuide"
                    placeholderText="User Guide"
                    beginUpload={(uploadId, fileName) =>
                      this.props.beginFileUpload({ uploadId, fileName })}
                    cancelFileUpload={(uploadId) =>
                      this.props.cancelFileUpload({ uploadId })}
                    removeExistingFile={(uploadId) =>
                      this.props.removeExistingFile({ uploadId })}
                    finalizeUpload={(uploadId, fileName, guid) =>
                      this.props.finalizeUpload({ uploadId, fileName, guid })}
                    setUploadError={(uploadId, errorMsg) =>
                      this.props.setUploadError({ uploadId, errorMsg })}
                    updateChecksumProgress={(uploadId, progress) =>
                      this.props.updateChecksumProgress({ uploadId, progress })}
                    updateUploadProgress={(uploadId, progress) =>
                      this.props.updateUploadProgress({ uploadId, progress })}
                    upload={uploads[pendingFormData.relatedFiles.UserGuide.uniqueUploadId]}
                    uploadId={pendingFormData.relatedFiles.UserGuide.uniqueUploadId}
                    fileUploadId={
                      (pendingFormData.relatedFiles.UserGuide.fileUploadId !== undefined)
                        ? pendingFormData.relatedFiles.UserGuide.fileUploadId
                        : null
                    }
                    value={pendingFormData.relatedFiles.UserGuide.fileOriginalName}
                    readOnly={formState === 'read'}
                  />
                  <FileUploadInput
                    fileExtensions={['pdf']}
                    label="Release Notes"
                    name="releaseNotes"
                    placeholderText="Release Notes"
                    beginUpload={(uploadId, fileName) =>
                      this.props.beginFileUpload({ uploadId, fileName })}
                    cancelFileUpload={(uploadId) =>
                      this.props.cancelFileUpload({ uploadId })}
                    removeExistingFile={(uploadId) =>
                      this.props.removeExistingFile({ uploadId })}
                    finalizeUpload={(uploadId, fileName, guid) =>
                      this.props.finalizeUpload({ uploadId, fileName, guid })}
                    setUploadError={(uploadId, errorMsg) =>
                      this.props.setUploadError({ uploadId, errorMsg })}
                    updateChecksumProgress={(uploadId, progress) =>
                      this.props.updateChecksumProgress({ uploadId, progress })}
                    updateUploadProgress={(uploadId, progress) =>
                      this.props.updateUploadProgress({ uploadId, progress })}
                    upload={uploads[pendingFormData.relatedFiles.ReleaseNotes.uniqueUploadId]}
                    uploadId={pendingFormData.relatedFiles.ReleaseNotes.uniqueUploadId}
                    fileUploadId={
                      (pendingFormData.relatedFiles.ReleaseNotes.fileUploadId !== undefined)
                        ? pendingFormData.relatedFiles.ReleaseNotes.fileUploadId
                        : null
                    }
                    value={pendingFormData.relatedFiles.ReleaseNotes.fileOriginalName}
                    readOnly={formState === 'read'}
                  />
                </FormFlexContainer>
              </FormSectionRow>
            </FormSection>
            {
              (formData.formState !== 'read'
                || (
                  pendingFormData.contentDescription &&
                  pendingFormData.contentDescription.length > 0
                )
              ) &&
              <FormSection title="Content Description">
                <FormSectionRow>
                  <FormFlexContainer flexPhone={12}>
                    <TextAreaInput
                      error={formData.formErrors.contentDescription}
                      label="Content Description"
                      name="contentDescription"
                      onBlur={() => false}
                      onChange={({ currentTarget: target }: React.FormEvent<HTMLTextAreaElement>) => {
                        this.props.setPublishingFormTextInputValue({
                          inputName: 'contentDescription',
                          value: target.value,
                        });
                      }}
                      placeholderText="Content Description..."
                      value={pendingFormData.contentDescription}
                      readOnly={formState === 'read'}
                    />
                  </FormFlexContainer>
                </FormSectionRow>
              </FormSection>
            }
            {
              (formData.formState !== 'read'
                || (
                  pendingFormData.contentDisclaimer &&
                  pendingFormData.contentDisclaimer.length > 0
                )
              ) &&
              <FormSection title="Custom Content Disclaimer">
                <FormSectionRow>
                  <FormFlexContainer flexPhone={12}>
                    {
                      formData.formState === 'write' &&
                      <TabRow
                        tabs={[{ id: 'edit', label: 'Edit' }, { id: 'preview', label: 'Preview' }]}
                        selectedTab={formData.disclaimerInputState}
                        onTabSelect={(tab: 'edit' | 'preview') => this.props.setDisclaimerInputState({value: tab})}
                        fullWidth={true}
                      />
                    }
                    {
                      formData.disclaimerInputState === 'preview' || formData.formState === 'read'
                        ? (
                          <div
                            className={`disclaimer-preview${formData.formState === 'read' ? ' disabled' : ''}`}
                            dangerouslySetInnerHTML={{
                              __html: convertMarkdownToHTML(pendingFormData.contentDisclaimer),
                            }}
                          />
                        ) : (
                          <>
                            <TextAreaInput
                              error={formData.formErrors.contentDisclaimer}
                              label="Custom Disclaimer Text"
                              name="contentDisclaimer"
                              onBlur={() => false}
                              onChange={({ currentTarget: target }: React.FormEvent<HTMLTextAreaElement>) => {
                                this.props.setPublishingFormTextInputValue({
                                  inputName: 'contentDisclaimer',
                                  value: target.value,
                                });
                              }}
                              placeholderText="Custom Disclaimer Text..."
                              value={pendingFormData.contentDisclaimer}
                              readOnly={formState === 'read'}
                            />
                            <div className="disclaimer-instructions">
                              **<strong>bold</strong>**, _<i>italics</i>_, ### <strong>Section Header</strong>
                            </div>
                          </>
                        )
                    }
                  </FormFlexContainer>
                </FormSectionRow>
              </FormSection>
            }
            {
              (formData.formState !== 'read'
                || (
                  pendingFormData.contentNotes &&
                  pendingFormData.contentNotes.length > 0
                )
              ) &&
              <FormSection title="Internal Notes (Not Shown To End Users)">
                <FormSectionRow>
                  <FormFlexContainer flexPhone={12}>
                    <TextAreaInput
                      error={formData.formErrors.contentNotes}
                      label="Notes"
                      name="contentNotes"
                      onBlur={() => false}
                      onChange={({ currentTarget: target }: React.FormEvent<HTMLTextAreaElement>) => {
                        this.props.setPublishingFormTextInputValue({
                          inputName: 'contentNotes',
                          value: target.value,
                        });
                      }}
                      placeholderText="Notes..."
                      value={pendingFormData.contentNotes}
                      readOnly={formState === 'read'}
                    />
                  </FormFlexContainer>
                </FormSectionRow>
              </FormSection>
            }
          </ContentPanelForm>
        </ContentPanelSectionContent>
        {
          formState === 'write' && (this.props.formChangesPending || this.props.uploadChangesPending) &&
          <div className="button-container">
            <button
              className="link-button"
              type="button"
              onClick={(event: any) => {
                event.preventDefault();
                this.props.openModifiedFormModal({
                  afterFormModal:
                  {
                    entityToSelect: null,
                    entityType: 'Undo Changes',
                  },
                });
              }}
            >
              Undo Changes
            </button>
            <button
              type="button"
              className={`green-button${this.props.formCanSubmit ? '' : ' disabled'}`}
              onClick={(event: React.MouseEvent) => {
                event.preventDefault();
                if (!pendingFormData.id) {
                  this.props.createNewContentItem(this.props.contentItemForPublication);
                } else {
                  if (this.props.formChangesPending) {
                    this.props.updateContentItem(this.props.contentItemForPublication);
                  }
                  if (this.props.uploadChangesPending) {
                    this.props.publishContentFiles(this.props.filesForPublishing);
                  }
                }
              }}
            >
              {`${pendingFormData.id ? 'Update' : 'Create'} Content Item`}
              {this.props.pending.data.formSubmit
                ? <ButtonSpinner version="circle" />
                : null
              }
            </button>
          </div>
        }
      </ContentPanel>
    );
  }

  private renderGoLiveSummary() {
    const {
      elementsToConfirm, goLiveSummary, rootContentItemId, onlyChangesShown,
    } = this.props.goLiveSummary;
    const contentCardPreview = goLiveSummary && (
      <ContentCard
        id={this.props.goLiveSummary.rootContentItemId}
        name={goLiveSummary.rootContentName}
        contentTypeEnum={contentTypeMap[goLiveSummary.contentTypeName]}
        description={goLiveSummary.contentDescription}
        contentURL={goLiveSummary.masterContentLink}
        imageURL={goLiveSummary.thumbnailLink}
        userguideURL={goLiveSummary.userGuideLink}
        releaseNotesURL={goLiveSummary.releaseNotesLink}
        editableEnabled={goLiveSummary.editableEnabled}
        selectContent={() => false}
      />
    );
    const masterContentPreview = goLiveSummary && goLiveSummary.masterContentLink && (
      <GoLiveSection
        title="Master Content"
        checkboxLabel="Master Content is as expected"
        checkboxTarget="masterContent"
        checkboxSelectedValue={elementsToConfirm.masterContent}
        checkboxFunction={this.props.toggleGoLiveConfirmationCheckbox}
      >
        {(goLiveSummary.contentTypeName === 'FileDownload') ? (
          <div className="download-preview">
            <a href={goLiveSummary.masterContentLink} download={true}>
              Download {goLiveSummary.rootContentName}
            </a>
          </div>
        ) : (
          <ContentContainer
            contentType={contentTypeMap[goLiveSummary.contentTypeName]}
            contentURL={goLiveSummary.masterContentLink}
          >
            <a
              href={goLiveSummary.masterContentLink}
              className="new-tab-icon"
              target="_blank"
              title="Open in new tab"
            >
              <svg className="action-icon-expand-frame action-icon tooltip">
                <use xlinkHref="#expand-frame" />
              </svg>
            </a>
          </ContentContainer>
        )}
      </GoLiveSection>
    );
    const thumbnailPreview = goLiveSummary && goLiveSummary.thumbnailLink && (
      <GoLiveSection
        title="Thumbnail"
        checkboxLabel="Thumbnail is as expected"
        checkboxTarget="thumbnail"
        checkboxSelectedValue={elementsToConfirm.thumbnail}
        checkboxFunction={this.props.toggleGoLiveConfirmationCheckbox}
      >
        <div className="thumbnailContainer">
          <img className="thumbnailPreview" src={goLiveSummary.thumbnailLink} />
        </div>
      </GoLiveSection>
    );
    const userGuidePreview = goLiveSummary && goLiveSummary.userGuideLink && (
      <GoLiveSection
        title="User Guide"
        checkboxLabel="User Guide is as expected"
        checkboxTarget="userguide"
        checkboxSelectedValue={elementsToConfirm.userguide}
        checkboxFunction={this.props.toggleGoLiveConfirmationCheckbox}
      >
        <ContentContainer
          contentType={ContentTypeEnum.Pdf}
          contentURL={goLiveSummary.userGuideLink}
        >
          <a
            href={goLiveSummary.userGuideLink}
            className="new-tab-icon"
            target="_blank"
            title="Open in new tab"
          >
            <svg className="action-icon-expand-frame action-icon">
              <use xlinkHref="#expand-frame" />
            </svg>
          </a>
        </ContentContainer>
      </GoLiveSection>
    );
    const releaseNotesPreview = goLiveSummary && goLiveSummary.releaseNotesLink && (
      <GoLiveSection
        title="Release Notes"
        checkboxLabel="Release Notes are as expected"
        checkboxTarget="releaseNotes"
        checkboxSelectedValue={elementsToConfirm.releaseNotes}
        checkboxFunction={this.props.toggleGoLiveConfirmationCheckbox}
      >
        <ContentContainer
          contentType={ContentTypeEnum.Pdf}
          contentURL={goLiveSummary.releaseNotesLink}
        >
          <a
            href={goLiveSummary.releaseNotesLink}
            className="new-tab-icon"
            target="_blank"
            title="Open in new tab"
          >
            <svg className="action-icon-expand-frame action-icon">
              <use xlinkHref="#expand-frame" />
            </svg>
          </a>
        </ContentContainer>
      </GoLiveSection>
    );
    const hierarchyChildren = goLiveSummary && goLiveSummary.reductionHierarchy && (
      <>
        <Toggle
          label="Show only changed values"
          checked={onlyChangesShown}
          onClick={() => this.props.toggleShowOnlyChanges({})}
        />
        <HierarchyDiffs
          changedOnly={onlyChangesShown}
          hierarchy={goLiveSummary.reductionHierarchy}
        />
      </>
    );
    const qlikViewHierarchyValues = goLiveSummary && goLiveSummary.reductionHierarchy
      && goLiveSummary.contentTypeName === 'Qlikview' && (
      <GoLiveSection
        title="Hierarchy Changes"
        checkboxLabel="All hierarchy changes are as expected"
        checkboxTarget="reductionHierarchy"
        checkboxSelectedValue={elementsToConfirm.reductionHierarchy}
        checkboxFunction={this.props.toggleGoLiveConfirmationCheckbox}
      >
        {hierarchyChildren}
      </GoLiveSection>
      );
    const powerBiHierarchyValues = goLiveSummary && goLiveSummary.reductionHierarchy
      && goLiveSummary.contentTypeName === 'PowerBi' && (
      <GoLiveSection
        title="Role Changes"
        checkboxLabel="All role changes are as expected"
        checkboxTarget="reductionHierarchy"
        checkboxSelectedValue={elementsToConfirm.reductionHierarchy}
        checkboxFunction={this.props.toggleGoLiveConfirmationCheckbox}
      >
        {hierarchyChildren}
      </GoLiveSection>
      );
    const hierarchyValues = goLiveSummary && goLiveSummary.contentTypeName === 'Qlikview' ? qlikViewHierarchyValues :
      ((goLiveSummary && goLiveSummary.contentTypeName) === 'PowerBi' ? powerBiHierarchyValues : null);
    const selectionGroups = goLiveSummary && goLiveSummary.selectionGroups && (
      <GoLiveSection
        title="Selection Groups"
        checkboxLabel="All reductions are as expected"
        checkboxTarget="selectionGroups"
        checkboxSelectedValue={elementsToConfirm.selectionGroups}
        checkboxFunction={this.props.toggleGoLiveConfirmationCheckbox}
      >
        {
          goLiveSummary.selectionGroups.map((sG, key) => (
            <SelectionGroupDetails
              selectionGroup={sG}
              changedOnly={false}
              key={key}
            />
          ),
        )}
      </GoLiveSection>
    );
    const attestationLanguage = goLiveSummary && goLiveSummary.attestationLanguage && (
      <>
        <h3>Attestation</h3>
        <div dangerouslySetInnerHTML={{ __html: goLiveSummary.attestationLanguage }} />
      </>
    );
    return (
      <ContentPanel loading={this.props.pending.data.goLiveSummary}>
        <h3 className="admin-panel-header">Pre-Live Summary</h3>
        <PanelSectionToolbar>
          <PanelSectionToolbarButtons>
            <ActionIcon
              label="Close Pre-Live Summary"
              icon="cancel"
              action={() => { this.props.selectItem({ id: null }); }}
            />
          </PanelSectionToolbarButtons>
        </PanelSectionToolbar>
        <ContentPanelSectionContent>
          <h2>{goLiveSummary && goLiveSummary.rootContentName}</h2>
          {masterContentPreview}
          {thumbnailPreview}
          {userGuidePreview}
          {releaseNotesPreview}
          {hierarchyValues}
          {selectionGroups}
          {attestationLanguage}
        </ContentPanelSectionContent>
        <div className="go-live-button-container">
          <button
            className="red-button"
            onClick={() => this.props.openGoLiveRejectionModal({})}
          >
            Reject
            {this.props.pending.data.goLiveRejection
              ? <ButtonSpinner version="circle" />
              : null
            }
          </button>
          <button
            className="green-button"
            disabled={!this.props.goLiveApproveButtonIsActive}
            onClick={() => this.props.approveGoLiveSummary({
              rootContentItemId,
              publicationRequestId: goLiveSummary.publicationRequestId,
              validationSummaryId: goLiveSummary.validationSummaryId,
            })}
          >
            Approve
            {this.props.pending.data.goLiveApproval
              ? <ButtonSpinner version="circle" />
              : null
            }
          </button>
        </div>
      </ContentPanel>
    );

  }
}

function mapStateToProps(state: PublishingState): ContentPublishingProps {
  const {
    data, formData, goLiveSummary, selected, cardAttributes, pending, filters, modals,
  } = state;
  const { id: rootContentItemId } = formData.pendingFormData;
  return {
    clients: clientEntities(state),
    items: itemEntities(state),
    contentTypes: data.contentTypes,
    contentAssociatedFileTypes: data.contentAssociatedFileTypes,
    contentTypesList: availableContentTypes(state),
    associatedContentTypesList: availableAssociatedContentTypes(state),
    formData,
    goLiveSummary,
    selected,
    cardAttributes,
    pending,
    filters,
    modals,
    selectedItem: selectedItem(state),
    activeSelectedClient: activeSelectedClient(state),
    activeSelectedItem: activeSelectedItem(state),
    filesForPublishing: filesForPublishing(state, rootContentItemId),
    formCanSubmit: submitButtonIsActive(state),
    contentItemToBeDeleted: contentItemToBeDeleted(state),
    contentItemToBeCanceled: contentItemToBeCanceled(state),
    formChangesPending: formChangesPending(state),
    goLiveApproveButtonIsActive: goLiveApproveButtonIsActive(state),
    uploadChangesPending: uploadChangesPending(state),
    contentItemForPublication: contentItemForPublication(state),
    canDownloadCurrentContentItem: canDownloadCurrentContentItem(state),
  };
}

export const ConnectedContentPublishing = connect(
  mapStateToProps,
  PublishingActionCreators,
)(ContentPublishing);
