import '../../../scss/react/content-publishing/content-publishing.scss';

import '../../../images/icons/add.svg';
import '../../../images/icons/expand-frame.svg';
import '../../../images/icons/user.svg';

import * as React from 'react';
import * as Modal from 'react-modal';
import { connect } from 'react-redux';
import ReduxToastr from 'react-redux-toastr';

import { setUnloadAlert } from '../../unload-alerts';
import {
  ContentTypeEnum, PublicationStatus, PublishRequest,
} from '../../view-models/content-publishing';
import { ContentCard } from '../authorized-content/content-card';
import {
  Client, ClientWithStats, ContentAssociatedFileType, ContentType,
  RootContentItem, RootContentItemWithPublication,
} from '../models';
import { ActionIcon } from '../shared-components/action-icon';
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
import { Input, TextAreaInput } from '../shared-components/form/input';
import { Select } from '../shared-components/form/select';
import { Toggle } from '../shared-components/form/toggle';
import { NavBar } from '../shared-components/navbar';
import { Dict } from '../shared-components/redux/store';
import { GoLiveSection } from './go-live-section';
import { HierarchyDiffs } from './hierarchy-diffs';
import * as PublishingActionCreators from './redux/action-creators';
import {
  activeSelectedClient, activeSelectedItem, availableAssociatedContentTypes,
  availableContentTypes, clientEntities, contentItemToBeDeleted, filesForPublishing,
  formChangesPending, goLiveApproveButtonIsActive, itemEntities, selectedItem,
  submitButtonIsActive, uploadChangesPending,
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
  formCanSubmit: boolean;
  formChangesPending: boolean;
  goLiveApproveButtonIsActive: boolean;
  uploadChangesPending: boolean;
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
        {this.renderClientPanel()}
        {selected.client && this.renderItemPanel()}
        {(goLiveSummary.rootContentItemId)
          ? this.renderGoLiveSummary()
          : (selected.item
            && formData.formData.clientId) ? this.renderContentItemForm() : null
        }
        <Modal
          isOpen={modals.contentItemDeletion.isOpen}
          onRequestClose={() => this.props.closeDeleteContentItemModal({})}
          ariaHideApp={false}
          className="modal"
          overlayClassName="modal-overlay"
          closeTimeoutMS={100}
        >
          <h3 className="title red">Delete Selection Group</h3>
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
                if (!this.props.pending.data.contentItemDeletion) {
                  this.props.deleteContentItem(this.props.pending.contentItemToDelete);
                }
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
              {this.props.pending.data.contentItemDeletion
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
                  case 'Edit Content Item':
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
                    name={'Reports'}
                    value={entity.contentItemCount}
                    icon={'reports'}
                  />
                  <CardStat
                    name={'Users'}
                    value={entity.userCount}
                    icon={'user'}
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
    return activeClient && (
      <CardPanel
        entities={items}
        loading={pending.data.items}
        renderEntity={(entity, key) => {
          const cardButtons = entity.status.requestStatus === PublicationStatus.Processed ?
            (
              <>
                <CardButton
                  color={'green'}
                  tooltip={'Approve'}
                  onClick={() => {
                    if (this.props.formChangesPending || this.props.uploadChangesPending) {
                      this.props.openModifiedFormModal({
                        afterFormModal:
                        {
                          entityToSelect: entity.id,
                          entityType: 'Go Live Summary',
                        },
                      });
                    } else {
                      this.props.fetchGoLiveSummary({ rootContentItemId: entity.id });
                    }
                  }}
                  icon={'checkmark'}
                />
              </>
            ) : entity.status.requestStatus === PublicationStatus.Queued
              || entity.status.requestStatus === PublicationStatus.Validating ? (
                <>
                  <CardButton
                    color={'red'}
                    tooltip={'Cancel'}
                    onClick={() => this.props.cancelPublicationRequest(entity.id)}
                    icon={'cancel'}
                  />
                </>
              ) : (
                <>
                  <CardButton
                    color={'red'}
                    tooltip={'Delete'}
                    onClick={() => {
                      if (this.props.formChangesPending || this.props.uploadChangesPending) {
                        this.props.openModifiedFormModal({
                          afterFormModal:
                          {
                            entityToSelect: entity.id,
                            entityType: 'Delete Content Item',
                          },
                        });
                      } else {
                        this.props.openDeleteContentItemModal({ id: entity.id });
                      }
                    }}
                    icon={'delete'}
                  />
                  <CardButton
                    color={'green'}
                    tooltip={'Edit'}
                    onClick={() => {
                      if (this.props.formChangesPending || this.props.uploadChangesPending) {
                        this.props.openModifiedFormModal({
                          afterFormModal:
                          {
                            entityToSelect: entity.id,
                            entityType: 'Edit Content Item',
                          },
                        });
                      } else {
                        if (selected.item !== entity.id) {
                          this.props.fetchContentItemDetail({ rootContentItemId: entity.id });
                          this.props.selectItem({ id: entity.id });
                        }
                        this.props.setContentItemFormState({ formState: 'write' });
                      }
                    }}
                    icon={'edit'}
                  />
                </>
              );
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
                    name={'Selection groups'}
                    value={entity.selectionGroupCount}
                    icon={'group'}
                  />
                  <CardStat
                    name={'Assigned users'}
                    value={entity.assignedUserCount}
                    icon={'user'}
                  />
                </CardSectionStats>
                <CardSectionButtons>
                  {cardButtons}
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
    const { contentTypes, formData: dataForForm, pending } = this.props;
    const { formErrors, formData, formState, uploads } = dataForForm;
    const editFormButton = (
      <ActionIcon
        label="Edit Content Item"
        icon="edit"
        action={() => { this.props.setContentItemFormState({ formState: 'write' }); }}
      />
    );
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
            {formState === 'read' &&
              editFormButton
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
                    name="contentName"
                    onBlur={() => false}
                    onChange={({ currentTarget: target }: React.FormEvent<HTMLInputElement>) => {
                      this.props.setPublishingFormTextInputValue({
                        inputName: 'contentName',
                        value: target.value,
                      });
                    }}
                    type="text"
                    value={formData.contentName}
                    readOnly={formState === 'read'}
                  />
                </FormFlexContainer>
                <FormFlexContainer flexPhone={12} flexTablet={5}>
                  <Select
                    error={dataForForm.formErrors.contentTypeId}
                    label="Content Type"
                    name="contentType"
                    onChange={({ currentTarget: target }: React.FormEvent<HTMLSelectElement>) => {
                      this.props.setPublishingFormTextInputValue({
                        inputName: 'contentTypeId',
                        value: target.value,
                      });
                    }}
                    placeholderText="Choose Content Type"
                    value={dataForForm.formData.contentTypeId}
                    values={this.props.contentTypesList}
                    readOnly={formState === 'read' || dataForForm.originalData.id.length > 0}
                  />
                </FormFlexContainer>
                <FormFlexContainer flexPhone={12} flexTablet={7}>
                  <FileUploadInput
                    fileExtensions={
                      contentTypes[dataForForm.formData.contentTypeId] !== undefined
                        ? contentTypes[dataForForm.formData.contentTypeId].fileExtensions
                        : []
                    }
                    label="Master Content"
                    name="masterContent"
                    placeholderText="Upload Master Content"
                    beginUpload={(uploadId, fileName) =>
                      this.props.beginFileUpload({ uploadId, fileName })}
                    cancelFileUpload={() => false}
                    finalizeUpload={(uploadId, fileName, Guid) =>
                      this.props.finalizeUpload({ uploadId, fileName, Guid })}
                    setUploadError={(uploadId, errorMsg) =>
                      this.props.setUploadError({ uploadId, errorMsg })}
                    updateChecksumProgress={(uploadId, progress) =>
                      this.props.updateChecksumProgress({ uploadId, progress })}
                    updateUploadProgress={(uploadId, progress) =>
                      this.props.updateUploadProgress({ uploadId, progress })}
                    upload={uploads[formData.relatedFiles.MasterContent.uniqueUploadId]}
                    uploadId={formData.relatedFiles.MasterContent.uniqueUploadId}
                    value={formData.relatedFiles.MasterContent.fileOriginalName}
                    readOnly={formState === 'read'}
                  />
                </FormFlexContainer>
              </FormSectionRow>
            </FormSection>
            {
              dataForForm.formData && dataForForm.formData.contentTypeId &&
              (
                contentTypes[dataForForm.formData.contentTypeId].displayName === 'QlikView' ||
                contentTypes[dataForForm.formData.contentTypeId].displayName === 'Power BI'
              ) &&
              <FormSection
                title={`${contentTypes[dataForForm.formData.contentTypeId].displayName} Specific Settings`}
              >
                {
                  contentTypes[dataForForm.formData.contentTypeId].displayName === 'QlikView' &&
                  <Checkbox
                    name="Reducible"
                    selected={dataForForm.formData.doesReduce}
                    onChange={(status) => this.props.setPublishingFormBooleanInputValue({
                      inputName: 'doesReduce',
                      value: status,
                    })}
                    readOnly={formState === 'read' || dataForForm.originalData.id.length > 0}
                  />
                }
                {
                  contentTypes[dataForForm.formData.contentTypeId].displayName === 'Power BI' &&
                  <>
                    <Checkbox
                      name="Navigation Pane"
                      selected={dataForForm.formData.typeSpecificDetailObject.navigationPaneEnabled}
                      onChange={(status) => this.props.setPublishingFormBooleanInputValue({
                        inputName: 'navigationPaneEnabled',
                        value: status,
                      })}
                      readOnly={formState === 'read'}
                    />
                    <Checkbox
                      name="Filter Pane"
                      selected={dataForForm.formData.typeSpecificDetailObject.filterPaneEnabled}
                      onChange={(status) => this.props.setPublishingFormBooleanInputValue({
                        inputName: 'filterPaneEnabled',
                        value: status,
                      })}
                      readOnly={formState === 'read'}
                    />
                    <Checkbox
                      name="Bookmark Pane"
                      selected={dataForForm.formData.typeSpecificDetailObject.bookmarksPaneEnabled}
                      onChange={(status) => this.props.setPublishingFormBooleanInputValue({
                        inputName: 'bookmarksPaneEnabled',
                        value: status,
                      })}
                      readOnly={formState === 'read'}
                    />
                  </>
                }
              </FormSection>
            }
            <FormSection title="Content Related Files">
              <FormSectionRow>
                <FormFlexContainer flexPhone={5}>
                  <FileUploadInput
                    fileExtensions={['jpg', 'jpeg', 'gif', 'png']}
                    label="Thumbnail"
                    name="thumbnail"
                    placeholderText="Upload Thumbnail"
                    beginUpload={(uploadId, fileName) =>
                      this.props.beginFileUpload({ uploadId, fileName })}
                    cancelFileUpload={() => false}
                    finalizeUpload={(uploadId, fileName, Guid) =>
                      this.props.finalizeUpload({ uploadId, fileName, Guid })}
                    setUploadError={(uploadId, errorMsg) =>
                      this.props.setUploadError({ uploadId, errorMsg })}
                    updateChecksumProgress={(uploadId, progress) =>
                      this.props.updateChecksumProgress({ uploadId, progress })}
                    updateUploadProgress={(uploadId, progress) =>
                      this.props.updateUploadProgress({ uploadId, progress })}
                    upload={uploads[formData.relatedFiles.Thumbnail.uniqueUploadId]}
                    uploadId={formData.relatedFiles.Thumbnail.uniqueUploadId}
                    value={formData.relatedFiles.Thumbnail.fileOriginalName}
                    readOnly={formState === 'read'}
                  />
                </FormFlexContainer>
                <FormFlexContainer flexPhone={7}>
                  <FileUploadInput
                    fileExtensions={['pdf']}
                    label="User Guide"
                    name="userGuide"
                    placeholderText="Upload User Guide"
                    beginUpload={(uploadId, fileName) =>
                      this.props.beginFileUpload({ uploadId, fileName })}
                    cancelFileUpload={() => false}
                    finalizeUpload={(uploadId, fileName, Guid) =>
                      this.props.finalizeUpload({ uploadId, fileName, Guid })}
                    setUploadError={(uploadId, errorMsg) =>
                      this.props.setUploadError({ uploadId, errorMsg })}
                    updateChecksumProgress={(uploadId, progress) =>
                      this.props.updateChecksumProgress({ uploadId, progress })}
                    updateUploadProgress={(uploadId, progress) =>
                      this.props.updateUploadProgress({ uploadId, progress })}
                    upload={uploads[formData.relatedFiles.UserGuide.uniqueUploadId]}
                    uploadId={formData.relatedFiles.UserGuide.uniqueUploadId}
                    value={formData.relatedFiles.UserGuide.fileOriginalName}
                    readOnly={formState === 'read'}
                  />
                  <FileUploadInput
                    fileExtensions={['pdf']}
                    label="Release Notes"
                    name="releaseNotes"
                    placeholderText="Upload Release Notes"
                    beginUpload={(uploadId, fileName) =>
                      this.props.beginFileUpload({ uploadId, fileName })}
                    cancelFileUpload={() => false}
                    finalizeUpload={(uploadId, fileName, Guid) =>
                      this.props.finalizeUpload({ uploadId, fileName, Guid })}
                    setUploadError={(uploadId, errorMsg) =>
                      this.props.setUploadError({ uploadId, errorMsg })}
                    updateChecksumProgress={(uploadId, progress) =>
                      this.props.updateChecksumProgress({ uploadId, progress })}
                    updateUploadProgress={(uploadId, progress) =>
                      this.props.updateUploadProgress({ uploadId, progress })}
                    upload={uploads[formData.relatedFiles.ReleaseNotes.uniqueUploadId]}
                    uploadId={formData.relatedFiles.ReleaseNotes.uniqueUploadId}
                    value={formData.relatedFiles.ReleaseNotes.fileOriginalName}
                    readOnly={formState === 'read'}
                  />
                </FormFlexContainer>
              </FormSectionRow>
            </FormSection>
            <FormSection title="Content Description">
              <FormSectionRow>
                <FormFlexContainer flexPhone={12}>
                  <TextAreaInput
                    error={dataForForm.formErrors.contentDescription}
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
                    value={dataForForm.formData.contentDescription}
                    readOnly={formState === 'read'}
                  />
                </FormFlexContainer>
                <FormFlexContainer flexPhone={12}>
                  <TextAreaInput
                    error={dataForForm.formErrors.contentDisclaimer}
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
                    value={dataForForm.formData.contentDisclaimer}
                    readOnly={formState === 'read'}
                  />
                </FormFlexContainer>
              </FormSectionRow>
            </FormSection>
            <FormSection title="Internal Notes (Not Shown To End Users)">
              <FormSectionRow>
                <FormFlexContainer flexPhone={12}>
                  <TextAreaInput
                    error={dataForForm.formErrors.contentNotes}
                    label="Notes"
                    name="contentDisclaimer"
                    onBlur={() => false}
                    onChange={({ currentTarget: target }: React.FormEvent<HTMLTextAreaElement>) => {
                      this.props.setPublishingFormTextInputValue({
                        inputName: 'contentNotes',
                        value: target.value,
                      });
                    }}
                    placeholderText="Notes..."
                    value={dataForForm.formData.contentNotes}
                    readOnly={formState === 'read'}
                  />
                </FormFlexContainer>
              </FormSectionRow>
            </FormSection>
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
                  disabled={!this.props.formCanSubmit}
                  onClick={(event: React.MouseEvent) => {
                    event.preventDefault();
                    if (!dataForForm.formData.id) {
                      this.props.createNewContentItem({
                        ClientId: dataForForm.formData.clientId,
                        ContentName: dataForForm.formData.contentName,
                        ContentTypeId: dataForForm.formData.contentTypeId,
                        DoesReduce: dataForForm.formData.doesReduce,
                        Description: dataForForm.formData.contentDescription,
                        ContentDisclaimer: dataForForm.formData.contentDisclaimer,
                        Notes: dataForForm.formData.contentNotes,
                      });
                    } else {
                      if (this.props.formChangesPending) {
                        this.props.updateContentItem({
                          Id: dataForForm.formData.id,
                          ClientId: dataForForm.formData.clientId,
                          ContentName: dataForForm.formData.contentName,
                          ContentTypeId: dataForForm.formData.contentTypeId,
                          DoesReduce: dataForForm.formData.doesReduce,
                          Description: dataForForm.formData.contentDescription,
                          ContentDisclaimer: dataForForm.formData.contentDisclaimer,
                          Notes: dataForForm.formData.contentNotes,
                        });
                      }
                      if (this.props.uploadChangesPending) {
                        this.props.publishContentFiles(this.props.filesForPublishing);
                      }
                    }
                  }}
                >
                  {`${dataForForm.formData.id ? 'Update' : 'Create'} Content Item`}
                  {this.props.pending.data.formSubmit
                    ? <ButtonSpinner version="circle" />
                    : null
                  }
                </button>
              </div>
            }
          </ContentPanelForm>
        </ContentPanelSectionContent>
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
              Click to Download
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
            <svg className="action-icon-expand-frame action-icon tooltip">
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
            <svg className="action-icon-expand-frame action-icon tooltip">
              <use xlinkHref="#expand-frame" />
            </svg>
          </a>
        </ContentContainer>
      </GoLiveSection>
    );
    const hierarchyValues = goLiveSummary && goLiveSummary.reductionHierarchy && (
      <GoLiveSection
        title="Hierarchy Changes"
        checkboxLabel="All hierarchy changes are as expected"
        checkboxTarget="reductionHierarchy"
        checkboxSelectedValue={elementsToConfirm.reductionHierarchy}
        checkboxFunction={this.props.toggleGoLiveConfirmationCheckbox}
      >
        <Toggle
          label="Show only changed values"
          checked={onlyChangesShown}
          onClick={() => this.props.toggleShowOnlyChanges({})}
        />
        <HierarchyDiffs
          changedOnly={onlyChangesShown}
          hierarchy={goLiveSummary.reductionHierarchy}
        />
      </GoLiveSection>
    );
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
              changedOnly={onlyChangesShown}
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
  const { id: rootContentItemId } = formData.formData;
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
    formChangesPending: formChangesPending(state),
    goLiveApproveButtonIsActive: goLiveApproveButtonIsActive(state),
    uploadChangesPending: uploadChangesPending(state),
  };
}

export const ConnectedContentPublishing = connect(
  mapStateToProps,
  PublishingActionCreators,
)(ContentPublishing);
